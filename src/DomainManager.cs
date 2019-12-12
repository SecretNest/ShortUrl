using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public static class DomainManager
    {
        static string HtmlFileName = SettingHost.ApplicationFolder + Path.DirectorySeparatorChar + "DomainManager.html";
        
        static Dictionary<string, Func<HttpContext, DomainSetting, OtherResult>> verbs = new Dictionary<string, Func<HttpContext, DomainSetting, OtherResult>>();

        static WeakReference<string> html = new WeakReference<string>(null);

        static DomainManager()
        {
            verbs.Add("GetDomainSetting", HttpGetDomainSetting); //Return: (200)PerDomainSetting
            verbs.Add("GetRedirects", HttpGetRedirects); //Return: (200)List<RedirectTargetWithDomainName>
            verbs.Add("UpdateDomainDefaultTarget", HttpUpdateDomainDefaultTarget); //Query: Target(string), Permanent(0/1), QueryProcess(0/1); Return: (200)RedirectTarget
            verbs.Add("UpdateDomainManagementKey", HttpUpdateDomainManagementKey); //Query: Key(string); Return: (204-WhenKeyIsSame), (205-WhenKeyIsChanged)
            verbs.Add("UpdateIgnoreCaseWhenMatching", HttpUpdateIgnoreCaseWhenMatching); //Query: IgnoreCase(0/1); Return: (204-WhenAllRedirectsAreAllKept), (205-WhenSomeRedirectsAreRemoved)
            verbs.Add("AddRedirect", HttpAddRedirect); //Query: DomainName(string), Target(string), Permanent(0/1), QueryProcess(0/1); Return: (200)RedirectTargetWithDomainName, (409-WhenExisting)
            verbs.Add("RemoveRedirect", HttpRemoveRedirect); //Query: DomainName(string); Return: (204), (410-WhenNotExisting)
            verbs.Add("UpdateRedirect", HttpUpdateRedirect); //Query: DomainName(string), NewDomainName(string, optional, only when changing name), Target(string), Permanent(0/1), QueryProcess(0/1); Return: (200)RedirectTargetWithDomainName, (409-WhenNewDomainNameExisting), (410-WhenDomainNameNotExisting)
        }

        static OtherResult HttpGetDomainSetting(HttpContext context, DomainSetting domain)
        {
            return new Status200Result<PerDomainSetting>(new PerDomainSetting
            {
                DefaultTarget = domain.DefaultTarget,
                ManagementKey = domain.ManagementKey,
                IgnoreCaseWhenMatching = domain.IgnoreCaseWhenMatching,
                IsHttps = context.Request.IsHttps
            });
        }

        static OtherResult HttpGetRedirects(HttpContext context, DomainSetting domain)
        {
            return new Status200Result<List<RedirectTargetWithDomainName>>(
                domain.Redirects.Select(i => new RedirectTargetWithDomainName(i.Key, i.Value))
                .OrderBy(i => i.DomainName, domain.IgnoreCaseWhenMatching ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList());
        }

        static OtherResult HttpUpdateDomainDefaultTarget(HttpContext context, DomainSetting domain)
        {
            var target = context.GetQueryTextParameter("Target");
            var permanent = context.GetQueryBooleanParameter("Permanent");
            var queryProcess = context.GetQueryBooleanParameter("QueryProcess");
            domain.DefaultTarget.Update(target, permanent, queryProcess);
            SettingHost.SaveSetting();
            return new Status200Result<RedirectTarget>(domain.DefaultTarget);
        }

        static OtherResult HttpUpdateDomainManagementKey(HttpContext context, DomainSetting domain)
        {
            var key = context.GetQueryTextParameter("Key");
            if (key == domain.ManagementKey)
            {
                return new Status204Result();
            }
            else
            {
                domain.ManagementKey = key;
                SettingHost.SaveSetting();
                return new Status205Result();
            }
        }

        static OtherResult HttpUpdateIgnoreCaseWhenMatching(HttpContext context, DomainSetting domain)
        {
            var ignoreCase = context.GetQueryBooleanParameter("IgnoreCase");
            if (ignoreCase == domain.IgnoreCaseWhenMatching)
            {
                return new Status204Result();
            }

            var result = domain.UpdateIgnoreCaseWhenMatching(ignoreCase);
            SettingHost.SaveSetting();
            if (result)
            {
                return new Status205Result();
            }
            else
            {
                return new Status204Result();
            }
        }

        static OtherResult HttpAddRedirect(HttpContext context, DomainSetting domain)
        {
            var domainName = context.GetQueryTextParameter("DomainName");
            var target = context.GetQueryTextParameter("Target");
            var permanent = context.GetQueryBooleanParameter("Permanent");
            var queryProcess = context.GetQueryBooleanParameter("QueryProcess");

            var redirect = RedirectTarget.Create(target, permanent, queryProcess);
            if (domain.Redirects.TryAdd(domainName, redirect))
            {
                SettingHost.SaveSetting();
                return new Status200Result<RedirectTargetWithDomainName>(new RedirectTargetWithDomainName(domainName, redirect));
            }
            else
            {
                return new Status409Result();
            }
        }

        static OtherResult HttpRemoveRedirect(HttpContext context, DomainSetting domain)
        {
            var domainName = context.GetQueryTextParameter("DomainName");
            if (domain.Redirects.Remove(domainName))
            {
                SettingHost.SaveSetting();
                return new Status204Result();
            }
            else
            {
                return new Status410Result();
            }
        }

        static OtherResult HttpUpdateRedirect(HttpContext context, DomainSetting domain)
        {
            var domainName = context.GetQueryTextParameter("DomainName");
            var newDomainName = context.GetQueryOptionalTextParameter("NewDomainName");
            var target = context.GetQueryTextParameter("Target");
            var permanent = context.GetQueryBooleanParameter("Permanent");
            var queryProcess = context.GetQueryBooleanParameter("QueryProcess");

            if (newDomainName != null && newDomainName != domainName)
            {
                //Change domain name
                if (domain.Redirects.ContainsKey(newDomainName))
                {
                    return new Status409Result();
                }
                else if (domain.Redirects.Remove(domainName))
                {
                    var redirect = RedirectTarget.Create(target, permanent, queryProcess);
                    domain.Redirects.Add(newDomainName, redirect);
                    SettingHost.SaveSetting();
                    return new Status200Result<RedirectTargetWithDomainName>(new RedirectTargetWithDomainName(domainName, redirect));
                }
                else
                {
                    return new Status410Result();
                }
            }
            else if (domain.Redirects.TryGetValue(domainName, out RedirectTarget redirect))
            {
                redirect.Update(target, permanent, queryProcess);
                return new Status200Result<RedirectTargetWithDomainName>(new RedirectTargetWithDomainName(domainName, redirect));
            }
            else
            {
                return new Status410Result();
            }
        }

        public static OtherResult DomainManage(HttpContext context, DomainSetting domain)
        {
            var verb = context.GetQueryOptionalTextParameter("Verb");
            if (verb != null && verbs.TryGetValue(verb, out var process))
            {
                return process(context, domain);
            }
            else
            {
                if (!html.TryGetTarget(out var data))
                {
                    data = File.ReadAllText(HtmlFileName);
                    html.SetTarget(data);
                }
                return new Status200Result(data, "text/html");
            }
        }
    }
}
