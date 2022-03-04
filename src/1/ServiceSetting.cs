using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SecretNest.ShortUrl
{
    public class ServiceSetting
    {
        //public string KestrelUrl { get; set; }
        public bool EnableStaticFiles { get; set; }
        public string UserSpecifiedStaticFileFolder { get; set; }
        public bool PreferXForwardedHost { get; set; }
        public RedirectTarget DefaultTarget { get; set; }

        public string GlobalManagementKey { get; set; }
        private const string DefaultGlobalManagementKey = "$$$$GlobalManagement$$$$";
        public HashSet<string> GlobalManagementEnabledHosts { get; set; }
        public Dictionary<string, DomainSetting> Domains { get; set; }
        public Dictionary<string, string> Aliases { get; set; }

        public static ServiceSetting CreateDefault()
        {
            var item = new ServiceSetting
            {
                //KestrelUrl = "http://localhost:40020",
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
                    ["localhost:8088"] = "localhost"
                }
            };
            return item;
        }

        public void FixAfterDeserializing()
        {
            GlobalManagementEnabledHosts = new HashSet<string>(GlobalManagementEnabledHosts, StringComparer.OrdinalIgnoreCase);

            Domains = new Dictionary<string, DomainSetting>(Domains, StringComparer.OrdinalIgnoreCase);
            foreach (var domain in Domains.Values.Where(domain => domain.IgnoreCaseWhenMatching))
            {
                domain.Redirects = new Dictionary<string, RedirectTarget>(domain.Redirects, StringComparer.OrdinalIgnoreCase);
            }

            Aliases = new Dictionary<string, string>(Aliases, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class DomainSetting
    {
        private const string DefaultManagementKey = "$$$$DomainManagement$$$$";
        public string ManagementKey { get; set; }
        public RedirectTarget DefaultTarget { get; set; }
        public bool IgnoreCaseWhenMatching { get; set; }
        public Dictionary<string, RedirectTarget> Redirects { get; set; }

        public static DomainSetting CreateEmpty(string managementKey = DefaultManagementKey)
        {
            var item = new DomainSetting
            {
                ManagementKey = managementKey,
                DefaultTarget = RedirectTarget.CreateEmpty(),
                IgnoreCaseWhenMatching = true,
                Redirects = new Dictionary<string, RedirectTarget>(StringComparer.OrdinalIgnoreCase)
            };
            return item;
        }

        public const string DefaultRecordKey = "localhost";
        public static DomainSetting CreateDefaultRecord()
        {
            var item = new DomainSetting
            {
                ManagementKey = DefaultManagementKey,
                DefaultTarget = RedirectTarget.CreateDefault(),
                IgnoreCaseWhenMatching = true,
                Redirects = new Dictionary<string, RedirectTarget>(StringComparer.OrdinalIgnoreCase)
                {
                    [RedirectTarget.DefaultRecordKey] = RedirectTarget.CreateDefaultRecord(),
                    [RedirectTarget.DefaultRecordKey2] = RedirectTarget.CreateDefaultRecord2()
                }
            };
            return item;
        }

        public bool UpdateIgnoreCaseWhenMatching(bool ignoreCaseWhenMatching)
        {
            IgnoreCaseWhenMatching = ignoreCaseWhenMatching;
            if (ignoreCaseWhenMatching)
            {
                var item = new Dictionary<string, RedirectTarget>(StringComparer.OrdinalIgnoreCase);
                foreach (var (segment, target) in Redirects)
                {
                    item[segment] = target;
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
            var item = new RedirectTarget { QueryProcess = RedirectQueryProcess.Ignored };
            return item;
        }

        public static RedirectTarget Create(string target, bool permanent, bool queryProcessRequired)
        {
            var item = new RedirectTarget();
            item.Update(target, permanent, queryProcessRequired);
            return item;
        }

        public void Update(string target, bool permanent, bool queryProcessRequired)
        {
            Target = target;
            Permanent = permanent;
            if (queryProcessRequired)
            {
                QueryProcess = target.Contains("?") ? RedirectQueryProcess.AppendRemovingLeadingQuestionMark : RedirectQueryProcess.AppendDirectly;
            }
            else
            {
                QueryProcess = RedirectQueryProcess.Ignored;
            }
        }

        public const string DefaultRecordKey = "github";
        public static RedirectTarget CreateDefaultRecord()
        {
            var item = new RedirectTarget()
            {
                Target = "https://www.github.com/",
                //Permanent = false,
                QueryProcess = RedirectQueryProcess.AppendDirectly
            };
            return item;
        }

        public const string DefaultRecordKey2 = "git";
        public static RedirectTarget CreateDefaultRecord2()
        {
            var item = new RedirectTarget()
            {
                Target = ">" + DefaultRecordKey
            };
            return item;
        }

        public static RedirectTarget CreateDefault()
        {
            var item = new RedirectTarget()
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
        AppendRemovingLeadingQuestionMark = 2
    }
}
