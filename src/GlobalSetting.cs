using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public class GlobalSetting
    {
        public RedirectTarget DefaultTarget { get; set; }
        public string GlobalManagementKey { get; set; }
        public List<string> GlobalManagementEnabledHosts { get; set; }
        public string CurrentHost { get; set; }
        //public bool IsHttps { get; set; }
    }

    public class DomainRecord
    {
        public string DomainName { get; set; }
        public string ManagementKey { get; set; }
    }

    public class AliasRecord
    {
        public string Alias { get; set; }
        public string Target { get; set; }
    }
}
