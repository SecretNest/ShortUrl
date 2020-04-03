using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace SecretNest.ShortUrl
{
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
    public class Program
#pragma warning restore CA1052 // Static holder types should be Static or NotInheritable
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    //if (!string.IsNullOrEmpty(SettingHost.ServiceSetting.KestrelUrl))
                    //{
                    //    webBuilder.UseUrls(SettingHost.ServiceSetting.KestrelUrl);
                    //}
                });
    }
}
