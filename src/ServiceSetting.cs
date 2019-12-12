using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public class ServiceSetting
    {
        public string KestrelUrl { get; set; }
        public bool EnableStaticFiles { get; set; }
        public string UserSpecifiedStaticFileFolder { get; set; }
        public bool PreferXForwardedHost { get; set; }
        public RedirectTarget DefaultTarget { get; set; }

        public string GlobalManagementKey { get; set; }
        public const string DefaultGlobalManagementKey = "$$$$GlobalManagement$$$$";
        public HashSet<string> GlobalManagementEnabledHosts { get; set; }
        public Dictionary<string, DomainSetting> Domains { get; set; }
        public Dictionary<string, string> Aliases { get; set; }

        public static ServiceSetting CreateDefault()
        {
            ServiceSetting item = new ServiceSetting
            {
                KestrelUrl = "http://localhost:40020",
                EnableStaticFiles = true,
                PreferXForwardedHost = true,
                DefaultTarget = RedirectTarget.CreateDefault(),
                GlobalManagementKey = DefaultGlobalManagementKey,
                GlobalManagementEnabledHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Domains = new Dictionary<string, DomainSetting>(StringComparer.OrdinalIgnoreCase)
                {
                    [DomainSetting.DefaultRecordKey] = DomainSetting.CreateDefaultRecord()
                },
                Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["git"] = "github"
                }
            };
            return item;
        }

        public void FixAfterDeserializing()
        {
            GlobalManagementEnabledHosts = new HashSet<string>(GlobalManagementEnabledHosts, StringComparer.OrdinalIgnoreCase); 

            Domains = new Dictionary<string, DomainSetting>(Domains, StringComparer.OrdinalIgnoreCase);
            foreach (var domain in Domains.Values)
            {
                if (domain.IgnoreCaseWhenMatching)
                {
                    domain.Redirects = new Dictionary<string, RedirectTarget>(domain.Redirects, StringComparer.OrdinalIgnoreCase);
                }
            }

            Aliases = new Dictionary<string, string>(Aliases, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class DomainSetting
    {
        public const string DefaultManagementKey = "$$$$DomainManagement$$$$";
        public string ManagementKey { get; set; }
        public RedirectTarget DefaultTarget { get; set; }
        public bool IgnoreCaseWhenMatching { get; set; }
        public Dictionary<string, RedirectTarget> Redirects { get; set; }

        public static DomainSetting CreateEmpty(string managementKey = DefaultManagementKey)
        {
            DomainSetting item = new DomainSetting
            {
                ManagementKey = managementKey,
                DefaultTarget = RedirectTarget.CreateEmpty(),
                IgnoreCaseWhenMatching = true,
                Redirects = new Dictionary<string, RedirectTarget>(StringComparer.OrdinalIgnoreCase)
            };
            return item;
        }

        public const string DefaultRecordKey = "example.com";
        public static DomainSetting CreateDefaultRecord()
        {
            DomainSetting item = new DomainSetting
            {
                ManagementKey = DefaultManagementKey,
                DefaultTarget = RedirectTarget.CreateDefault(),
                IgnoreCaseWhenMatching = true,
                Redirects = new Dictionary<string, RedirectTarget>(StringComparer.OrdinalIgnoreCase)
                {
                    [RedirectTarget.DefaultRecordKey] = RedirectTarget.CreateDefaultRecord()
                }
            };
            return item;
        }

        public bool UpdateIgnoreCaseWhenMatching(bool ignoreCaseWhenMatching)
        {
            if (ignoreCaseWhenMatching)
            {
                var item = new Dictionary<string, RedirectTarget>(StringComparer.OrdinalIgnoreCase);
                foreach(var record in Redirects)
                {
                    item[record.Key] = record.Value;
                }
                if (item.Count != Redirects.Count)
                {
                    Redirects = item;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Redirects = new Dictionary<string, RedirectTarget>(Redirects);
                return false;
            }
        }
    }

    public class RedirectTarget
    {
        public string Target { get; set; }
        public bool Permanent { get; set; }

        [DefaultValue(RedirectQueryProcess.Ignored)]
        public RedirectQueryProcess QueryProcess { get; set; }

        public static RedirectTarget CreateEmpty()
        {
            RedirectTarget item = new RedirectTarget { QueryProcess = RedirectQueryProcess.Ignored };
            return item;
        }

        public static RedirectTarget Create(string target, bool permanent, bool queryProcessRequired)
        {
            RedirectTarget item = new RedirectTarget();
            item.Update(target, permanent, queryProcessRequired);
            return item;
        }

        public void Update(string target, bool permanent, bool queryProcessRequired)
        {
            Target = target;
            Permanent = permanent;
            if (queryProcessRequired)
            {
                if (target.Contains("?"))
                {
                    QueryProcess = RedirectQueryProcess.AppendAfterExisted;
                }
                else
                {
                    QueryProcess = RedirectQueryProcess.AppendDirectly;
                }
            }
            else
            {
                QueryProcess = RedirectQueryProcess.Ignored;
            }
        }

        public const string DefaultRecordKey = "github";
        public static RedirectTarget CreateDefaultRecord()
        {
            RedirectTarget item = new RedirectTarget()
            {
                Target = "https://www.github.com/",
                //Permanent = false,
                QueryProcess = RedirectQueryProcess.AppendDirectly
            };
            return item;
        }

        public static RedirectTarget CreateDefault()
        {
            RedirectTarget item = new RedirectTarget()
            {
                Target = "https://www.google.com/",
                //Permanent = false,
                QueryProcess = RedirectQueryProcess.Ignored
            };
            return item;
        }
    }

    [DefaultValue(Ignored)]
    public enum RedirectQueryProcess : int
    {
        Ignored = 0,
        AppendDirectly = 1,
        AppendAfterExisted = 2
    }
}
