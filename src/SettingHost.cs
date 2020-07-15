using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;

namespace SecretNest.ShortUrl
{
    public static class SettingHost
    {
        internal static string ApplicationFolder { get; }

        static string ServiceSettingFileName { get; }

        static string GetApplicationFolder()
        {
            var exePath = System.Reflection
                   .Assembly.GetExecutingAssembly().CodeBase.Substring(7);
            return Path.GetDirectoryName(exePath);
        }

        public static ServiceSetting ServiceSetting { get; }

        static SettingHost()
        {
            ApplicationFolder = GetApplicationFolder();
            ServiceSettingFileName = ApplicationFolder + Path.DirectorySeparatorChar + "SecretNest.ShortUrl.Setting.json";

            if (File.Exists(ServiceSettingFileName))
            {
                Console.WriteLine("Reading from configuration file: " + ServiceSettingFileName);
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

        static readonly DefaultContractResolver contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.Indented
        };

        public static void SaveSetting()
        {
            Console.WriteLine("Saving to configuration file: " + ServiceSettingFileName);
            var fileData = JsonConvert.SerializeObject(ServiceSetting, jsonSerializerSettings);
            if (File.Exists(ServiceSettingFileName))
            {
                File.Move(ServiceSettingFileName, ServiceSettingFileName + ".bak", true);
            }
            File.WriteAllText(ServiceSettingFileName, fileData);
        }
    }
}
