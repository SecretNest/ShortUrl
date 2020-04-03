using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public static class GlobalManager
    {
        static readonly string HtmlFileName = SettingHost.ApplicationFolder + Path.DirectorySeparatorChar + "GlobalManager.html";

        static readonly Dictionary<string, Func<HttpContext, OtherResult>> verbs = new Dictionary<string, Func<HttpContext, OtherResult>>();

#if !DEBUG
        static readonly WeakReference<string> html = new WeakReference<string>(null);
#endif

        static GlobalManager()
        {
            verbs.Add("GetGlobalSetting", HttpGetGlobalSetting); //Return: (200)GlobalSetting
            verbs.Add("GetCurrentHost", HttpGetCurrentHost); //Return: (200)HostName(string)
            verbs.Add("GetDomains", HttpGetDomains); //Return: (200)List<DomainRecord>
            verbs.Add("GetAliases", HttpGetAliases); //Return: (200)List<AliasRecord>
            verbs.Add("UpdateGlobalDefaultTarget", HttpUpdateGlobalDefaultTarget); //Query: Target(string), Permanent(0/1), QueryProcess(0/1); Return: (200)RedirectTarget
            verbs.Add("UpdateGlobalManagementKey", HttpUpdateGlobalManagementKey); //Query: Key(string); Return: (204-WhenKeyIsSame), (200-WhenKeyIsChanged)NewKey(string)
            verbs.Add("AddGlobalManagementEnabledHost", HttpAddGlobalManagementEnabledHost); //Query: HostName(string); Return: (204), (406-WhenNotAcceptable: The 1st one added must be the current host.), (409-WhenExisting)
            verbs.Add("RemoveGlobalManagementEnabledHost", HttpRemoveGlobalManagementEnabledHost); //Query: HostName(string); Return: (204), (406-WhenNotAcceptable: The last one to be removed must be the current host.), (410-WhenNotExisting)
            verbs.Add("AddDomain", HttpAddDomain); //Query: DomainName(string); Return: (200)DomainRecord, (409-WhenExistingInDomainOrAlias)
            verbs.Add("RemoveDomain", HttpRemoveDomain); //Query: DomainName(string); Return: (204), (410-WhenNotExisting)
            verbs.Add("AddAlias", HttpAddAlias); //Query: Alias(string), Target(string); Return: (200)AliasRecord, (409-WhenExistingInDomainOrAlias)
            verbs.Add("RemoveAlias", HttpRemoveAlias); //Query: Alias(string); Return: (204), (410-WhenNotExisting)
            verbs.Add("UpdateAlias", HttpUpdateAlias); //Query: Alias(string), NewAlias(string, optional, only when changing alias), Target(string); Return: (200)AliasRecord, (409-WhenNewAliasExisting), (410-WhenAliasNotExisting)
        }

        static OtherResult HttpGetGlobalSetting(HttpContext context)
        {
            return new Status200Result<GlobalSetting>(new GlobalSetting
            {
                DefaultTarget = SettingHost.ServiceSetting.DefaultTarget,
                GlobalManagementKey = SettingHost.ServiceSetting.GlobalManagementKey,
                GlobalManagementEnabledHosts = SettingHost.ServiceSetting.GlobalManagementEnabledHosts
                    .OrderBy(i => i, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                CurrentHost = context.GetHost()//,
                //IsHttps = context.Request.IsHttps
            });
        }

        static OtherResult HttpGetCurrentHost(HttpContext context)
        {
            return new Status200Result(context.GetHost(), "text/plain");
        }

        static OtherResult HttpGetDomains(HttpContext context)
        {
            return new Status200Result<List<DomainRecord>>(new List<DomainRecord>
                (
                    SettingHost.ServiceSetting.Domains
                        .Select(i => new DomainRecord { DomainName = i.Key, ManagementKey = i.Value.ManagementKey })
                        .OrderBy(i => i.DomainName, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                ));
        }

        static OtherResult HttpGetAliases(HttpContext context)
        {
            return new Status200Result<List<AliasRecord>>(new List<AliasRecord>
                (
                    SettingHost.ServiceSetting.Aliases
                        .Select(i => new AliasRecord { Alias = i.Key, Target = i.Value })
                        .OrderBy(i => i.Alias, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                ));
        }

        static OtherResult HttpUpdateGlobalDefaultTarget(HttpContext context)
        {
            var target = context.GetQueryTextParameter("Target");
            var permanent = context.GetQueryBooleanParameter("Permanent");
            var queryProcess = context.GetQueryBooleanParameter("QueryProcess");
            SettingHost.ServiceSetting.DefaultTarget.Update(target, permanent, queryProcess);
            SettingHost.SaveSetting();
            return new Status200Result<RedirectTarget>(SettingHost.ServiceSetting.DefaultTarget);
        }

        static OtherResult HttpUpdateGlobalManagementKey(HttpContext context)
        {
            var key = context.GetQueryTextParameter("Key");
            if (key == SettingHost.ServiceSetting.GlobalManagementKey)
            {
                return new Status204Result();
            }
            else
            {
                SettingHost.ServiceSetting.GlobalManagementKey = key;
                SettingHost.SaveSetting();
                return new Status200Result(key, "text/plain");
            }
        }

        static OtherResult HttpAddGlobalManagementEnabledHost(HttpContext context)
        {
            var hostName = context.GetQueryTextParameter("HostName");

            if (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Count == 0)
            {
                var currentHost = context.GetHost();
                if (string.Equals(hostName, currentHost, StringComparison.OrdinalIgnoreCase))
                {
                    SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Add(hostName);
                    SettingHost.SaveSetting();
                    return new Status204Result();
                }
                else
                {
                    return new Status406Result();
                }
            }
            else if (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Add(hostName))
            {
                SettingHost.SaveSetting();
                return new Status204Result();
            }
            else
            {
                return new Status409Result();
            }
        }

        static OtherResult HttpRemoveGlobalManagementEnabledHost(HttpContext context)
        {
            var hostName = context.GetQueryTextParameter("HostName");

            if (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Count == 1)
            {
                var currentHost = context.GetHost();
                if (string.Equals(hostName, currentHost, StringComparison.OrdinalIgnoreCase)) //The last one must be the currentHost, or this method will not be called.
                {
                    SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Clear();
                    SettingHost.SaveSetting();
                    return new Status204Result();
                }
                else
                {
                    return new Status410Result();
                }
            }
            else if (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Count == 0)
            {
                return new Status410Result();
            }
            else // Count > 1
            {
                var currentHost = context.GetHost();
                if (string.Equals(hostName, currentHost, StringComparison.OrdinalIgnoreCase))
                {
                    return new Status406Result();
                }
                else if (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Remove(hostName))
                {
                    SettingHost.SaveSetting();
                    return new Status204Result();
                }
                else
                {
                    return new Status410Result();
                }
            }
        }

        static OtherResult HttpAddDomain(HttpContext context)
        {
            string domainName = context.GetQueryTextParameter("DomainName");

            if (SettingHost.ServiceSetting.Aliases.ContainsKey(domainName))
            {
                return new Status409Result();
            }

            var domain = DomainSetting.CreateEmpty();

            if (SettingHost.ServiceSetting.Domains.TryAdd(domainName, domain))
            {
                SettingHost.SaveSetting();
                return new Status200Result<DomainRecord>(new DomainRecord
                {
                    DomainName = domainName,
                    ManagementKey = domain.ManagementKey
                });
            }
            else
            {
                return new Status409Result();
            }
        }

        static OtherResult HttpRemoveDomain(HttpContext context)
        {
            string domainName = context.GetQueryTextParameter("DomainName");

            if (SettingHost.ServiceSetting.Domains.Remove(domainName))
            {
                SettingHost.SaveSetting();
                return new Status204Result();
            }
            else
            {
                return new Status410Result();
            }
        }

        static OtherResult HttpAddAlias(HttpContext context)
        {
            string alias = context.GetQueryTextParameter("Alias");
            string target = context.GetQueryTextParameter("Target");

            if (SettingHost.ServiceSetting.Domains.ContainsKey(alias))
            {
                return new Status409Result();
            }

            if (SettingHost.ServiceSetting.Aliases.TryAdd(alias, target))
            {
                SettingHost.SaveSetting();
                return new Status200Result<AliasRecord>(new AliasRecord
                {
                    Alias = alias,
                    Target = target
                });
            }
            else
            {
                return new Status409Result();
            }
        }

        static OtherResult HttpRemoveAlias(HttpContext context)
        {
            string alias = context.GetQueryTextParameter("Alias");

            if (SettingHost.ServiceSetting.Aliases.Remove(alias))
            {
                SettingHost.SaveSetting();
                return new Status204Result();
            }
            else
            {
                return new Status410Result();
            }
        }

        static OtherResult HttpUpdateAlias(HttpContext context)
        {
            string alias = context.GetQueryTextParameter("Alias");
            string target = context.GetQueryTextParameter("Target");

            string newAlias = context.GetQueryTextParameter("NewAlias");

            if (newAlias != null && newAlias != alias)
            {
                //Change alias key
                if (SettingHost.ServiceSetting.Domains.ContainsKey(newAlias) || SettingHost.ServiceSetting.Aliases.ContainsKey(newAlias))
                {
                    return new Status409Result();
                }
                else if (SettingHost.ServiceSetting.Aliases.Remove(alias))
                {
                    SettingHost.ServiceSetting.Aliases[newAlias] = target;
                    SettingHost.SaveSetting();
                    return new Status200Result<AliasRecord>(new AliasRecord
                    {
                        Alias = newAlias,
                        Target = target
                    });
                }
                else
                {
                    return new Status410Result();
                }
            }
            else if (SettingHost.ServiceSetting.Aliases.TryUpdate(alias, target))
            {
                SettingHost.SaveSetting();
                return new Status200Result<AliasRecord>(new AliasRecord
                {
                    Alias = alias,
                    Target = target
                });
            }
            else
            {
                return new Status410Result();
            }
        }

        static bool TryUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
                return true;
            }
            else
            {
                return false;
            }
        }

        public static OtherResult GlobalManage(HttpContext context)
        {
            var verb = context.GetQueryOptionalTextParameter("Verb");
            if (verb != null && verbs.TryGetValue(verb, out var process))
            {
                return process(context);
            }
            else
            {
#if DEBUG
                string data = File.ReadAllText(HtmlFileName);
#else
                if (!html.TryGetTarget(out var data))
                {
                    data = File.ReadAllText(HtmlFileName);
                    html.SetTarget(data);
                }
#endif
                return new Status200Result(data, "text/html");
            }
        }
    }
}
