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

    public class RedirectTargetWithAddress : RedirectTarget
    {
        public string Address { get; set; }

        public RedirectTargetWithAddress() { }

        public RedirectTargetWithAddress(string address, RedirectTarget target)
        {
            Address = address;
            Target = target.Target;
            Permanent = target.Permanent;
            QueryProcess = target.QueryProcess;
        }
    }
}
