using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public static class SettingHost
    {
        internal static string ApplicationFolder { get; }

        static string ServiceSettingFileName { get; }

        static string GetApplicationFolder()
        {
            var exePath = System.Reflection
                   .Assembly.GetExecutingAssembly().CodeBase.Substring(8);
            return Path.GetDirectoryName(exePath);
        }

        public static ServiceSetting ServiceSetting { get; }

        static SettingHost()
        {
            ApplicationFolder = GetApplicationFolder();
            ServiceSettingFileName = ApplicationFolder +Path.DirectorySeparatorChar + "SecretNest.ShortUrl.Setting.json";

            if (File.Exists(ServiceSettingFileName))
            {
                Console.WriteLine("Reading from config file: " + ServiceSettingFileName);
                var fileData = File.ReadAllText(ServiceSettingFileName);
                ServiceSetting = JsonConvert.DeserializeObject<ServiceSetting>(fileData);

                ServiceSetting.FixAfterDeserializing();
            }
            else
            {
                ServiceSetting = ServiceSetting.CreateDefault();
                SaveSetting();
            }
        }

        public static void SaveSetting()
        {
            Console.WriteLine("Saving to config file: " + ServiceSettingFileName);
            var fileData = JsonConvert.SerializeObject(ServiceSetting);
            if (File.Exists(ServiceSettingFileName))
            {
                File.Move(ServiceSettingFileName, ServiceSettingFileName + ".bak", true);
            }
            File.WriteAllText(ServiceSettingFileName, fileData);
        }
    }
}
