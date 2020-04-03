using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SecretNest.ShortUrl
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0060 // Remove unused parameter
        public void ConfigureServices(IServiceCollection services)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CA1822 // Mark members as static
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#pragma warning disable CA1822 // Mark members as static
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
#pragma warning restore CA1822 // Mark members as static
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            if (SettingHost.ServiceSetting.EnableStaticFiles)
            {
                if (string.IsNullOrEmpty(SettingHost.ServiceSetting.UserSpecifiedStaticFileFolder))
                {
                    app.UseStaticFiles();
                }
                else
                {
                    StaticFileOptions staticFileOptions = new StaticFileOptions()
                    {
                        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(SettingHost.ServiceSetting.UserSpecifiedStaticFileFolder)
                    };
                    app.UseStaticFiles(staticFileOptions);
                }
            }

            app.UseRouting();

            app.Use(ContextProcessFacade.Process);
        }
    }
}
