using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public static class ContextProcessFacade
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static async Task Process(HttpContext context, Func<Task> nextHandler)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var host = context.GetHost();
            var address = context.GetAccessKey();

            if (SettingHost.ServiceSetting.GlobalManagementKey == address &&
                (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Count == 0 || SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Contains(host)))
            {
                //Global Management
                try
                {
                    var result = GlobalManager.GlobalManage(context);
                    await context.ProcessOtherResultAsync(result);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
                {
                    await context.ProcessOtherResultAsync(new Status500Result());
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
            else
            {
                //Alias remap
                if (TryRemapAlias(host, out string aliasTarget))
                {
                    host = aliasTarget;
                }

                if (SettingHost.ServiceSetting.Domains.TryGetValue(host, out DomainSetting domain))
                {
                    //Domain matched
                    if (address == domain.ManagementKey)
                    {
                        //Domain Management
                        try
                        {
                            var result = DomainManager.DomainManage(context, domain);
                            await context.ProcessOtherResultAsync(result);
                        }
#pragma warning disable CA1031 // Do not catch general exception types
                        catch
                        {
                            await context.ProcessOtherResultAsync(new Status500Result());
                        }
#pragma warning restore CA1031 // Do not catch general exception types
                    }
                    else if (TryLocateRedirect(address, domain.Redirects, out RedirectTarget target))
                    {
                        //Record matched
                        context.Redirect(target);
                    }
                    else
                    {
                        //Domain default
                        if (string.IsNullOrEmpty(domain.DefaultTarget.Target))
                        {
                            context.Redirect(SettingHost.ServiceSetting.DefaultTarget);
                        }
                        else
                        {
                            context.Redirect(domain.DefaultTarget);
                        }
                    }
                }
                else
                {
                    //Global default
                    context.Redirect(SettingHost.ServiceSetting.DefaultTarget);
                }
            }
        }

        static bool TryRemapAlias(string alias, out string target)
        {
            if (SettingHost.ServiceSetting.Aliases.TryGetValue(alias, out string aliasTarget))
            {
                const int MaxRedirect = 16;
                int maxRedirect = MaxRedirect;
                alias = aliasTarget;

                while (SettingHost.ServiceSetting.Aliases.TryGetValue(alias, out aliasTarget))
                {
                    if (maxRedirect == 0)
                    {
                        //max redirect exceeded
                        target = null;
                        return false;
                    }
                    else
                    {
                        alias = aliasTarget;
                        maxRedirect--;
                    }
                }

                target = alias;
                return true;
            }
            else
            {
                //No Alias
                target = null;
                return false;
            }
        }

        static bool TryLocateRedirect(string address, Dictionary<string, RedirectTarget> records, out RedirectTarget target)
        {
            if (records.TryGetValue(address, out RedirectTarget redirectTarget))
            {
                if (redirectTarget.Target.StartsWith(">"))
                {
                    const int MaxRedirect = 16;
                    int maxRedirect = MaxRedirect;
                    address = redirectTarget.Target.Substring(1);

                    while (records.TryGetValue(address, out redirectTarget))
                    {
                        if (redirectTarget.Target.StartsWith(">"))
                        {
                            if (maxRedirect == 0)
                            {
                                //max redirect exceeded
                                target = null;
                                return false;
                            }
                            else
                            {
                                address = redirectTarget.Target.Substring(1);
                                maxRedirect--;
                            }
                        }
                        else
                        {
                            //redirect remap found
                            target = redirectTarget;
                            return true;
                        }
                    }

                    //redirect remap not found
                    target = null;
                    return false;
                }
                else
                {
                    //no remap
                    target = redirectTarget;
                    return true;
                }
            }
            else
            {
                //No record
                target = null;
                return false;
            }
        }

        internal static string GetHost(this HttpContext context)
        {
            if (SettingHost.ServiceSetting.PreferXForwardedHost)
            {
                var xForwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xForwardedHost))
                {
                    return xForwardedHost;
                }
            }
            if (context.Request.Host.Port != null && context.Request.Host.Port != 80 && context.Request.Host.Port != 443)
            {
                return string.Format("{0}:{1}", context.Request.Host.Host, context.Request.Host.Port);
            }
            else
            {
                return context.Request.Host.Value;
            }
        }

        internal static string GetQueryTextParameter(this HttpContext context, string parameter)
        {
            return context.Request.GetQueryTextParameter(parameter);
        }

        internal static string GetQueryOptionalTextParameter(this HttpContext context, string parameter)
        {
            return context.Request.GetQueryOptionalTextParameter(parameter);
        }

        internal static bool GetQueryBooleanParameter(this HttpContext context, string parameter)
        {
            return context.Request.GetQueryBooleanParameter(parameter);
        }

        internal static string GetQueryTextParameter(this HttpRequest request, string parameter)
        {
            return request.Query[parameter].First();
        }

        internal static string GetQueryOptionalTextParameter(this HttpRequest request, string parameter)
        {
            return request.Query[parameter].FirstOrDefault();
        }

        internal static bool GetQueryBooleanParameter(this HttpRequest request, string parameter)
        {
            var value = request.Query[parameter].FirstOrDefault();
            if (value == null) return false;
            else if (value == "1") return true;
            else if (value == "0") return false;
            else return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        static string GetAccessKey(this HttpContext context)
        {
            if (context.Request.Path.HasValue)
            {
                return context.Request.Path.Value.Substring(1);
            }
            else
            {
                return null;
            }
        }

        static void Redirect(this HttpContext context, RedirectTarget target)
        {
            var url = target.Target;

            var query = context.Request.QueryString;
            if (query.HasValue && query.Value.Length > 0)
            {
                if (target.QueryProcess == RedirectQueryProcess.AppendDirectly)
                {
                    url += query.Value;
                }
                else if (target.QueryProcess == RedirectQueryProcess.AppendRemovingLeadingQuestionMark)
                {
                    if (query.Value.StartsWith("?"))
                        url += "&" + query.Value.Substring(1);
                    else
                        url += "&" + query.Value;
                }
            }

            context.Response.Headers[HeaderNames.Location] = url;

            if (target.Permanent)
            {
                context.Response.StatusCode = StatusCodes.Status308PermanentRedirect;
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status307TemporaryRedirect;
            }
        }

        static async Task ProcessOtherResultAsync(this HttpContext context, HttpResponseResult result)
        {
            context.Response.StatusCode = result.StatusCode;
            if (result.HasContent)
            {
                context.Response.ContentType = result.ContentType;
                await context.Response.WriteAsync(result.Context);
            }
        }
    }
}
