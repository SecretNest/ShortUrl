using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public class PerDomainSetting
    {
        public string ManagementKey { get; set; }
        public RedirectTarget DefaultTarget { get; set; }
        public bool IgnoreCaseWhenMatching { get; set; }
        public bool IsHttps { get; set; }
    }

    public class RedirectTargetWithDomainName : RedirectTarget
    {
        public string DomainName { get; set; }

        public RedirectTargetWithDomainName() { }

        public RedirectTargetWithDomainName(string domainName, RedirectTarget target)
        {
            DomainName = domainName;
            Target = target.Target;
            Permanent = target.Permanent;
            QueryProcess = target.QueryProcess;
        }
    }
}
