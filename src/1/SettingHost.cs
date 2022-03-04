using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Reflection;

namespace SecretNest.ShortUrl
{
    public static class SettingHost
    {
        internal static string ApplicationFolder { get; }

        static string ServiceSettingFileName { get; }

        static string GetSettingFolder()
        {
            var env = Environment.GetEnvironmentVariable("SettingFolder");
            if (string.IsNullOrWhiteSpace(env))
            {
                env = ApplicationFolder;
            }
            else
            {
                env = Path.Combine(env, ApplicationFolder);

                Directory.CreateDirectory(env);
            }

            return env;
        }

        static string GetApplicationFolder()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(location);
        }

        public static ServiceSetting ServiceSetting { get; }

        static SettingHost()
        {
            ApplicationFolder = GetApplicationFolder();
            ServiceSettingFileName = GetSettingFolder() + Path.DirectorySeparatorChar + "SecretNest.ShortUrl.Setting.json";

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

        private static readonly DefaultContractResolver ContractResolver = new ()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        private static readonly JsonSerializerSettings JsonSerializerSettings = new ()
        {
            ContractResolver = ContractResolver,
            Formatting = Formatting.Indented
        };

        public static void SaveSetting()
        {
            Console.WriteLine("Saving to configuration file: " + ServiceSettingFileName);
            var fileData = JsonConvert.SerializeObject(ServiceSetting, JsonSerializerSettings);
            if (File.Exists(ServiceSettingFileName))
            {
                File.Move(ServiceSettingFileName, ServiceSettingFileName + ".bak", true);
            }
            File.WriteAllText(ServiceSettingFileName, fileData);
        }
    }
}
