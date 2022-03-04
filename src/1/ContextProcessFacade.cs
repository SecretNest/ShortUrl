using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecretNest.ShortUrl
{
    public static class ContextProcessFacade
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public static async Task Process(HttpContext context, Func<Task> nextHandler)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            await context.ProcessFlow();
            await context.Response.CompleteAsync();
        }

        static async Task ProcessFlow(this HttpContext context)
        {
            var host = context.GetHost();
            var address = context.GetAccessKey();

            if (SettingHost.ServiceSetting.GlobalManagementKey == address &&
                (SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Count == 0 ||
                 SettingHost.ServiceSetting.GlobalManagementEnabledHosts.Contains(host)))
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
                return;
            }

            //Alias remap
            if (TryRemapAlias(host, out var aliasTarget))
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
                    return;
                }

                if (TryLocateRedirect(address, domain.Redirects, out RedirectTarget target))
                {
                    //Record matched
                    if (context.Redirect(target))
                    {
                        return;
                    }
                }

                //Domain default
                if (!string.IsNullOrEmpty(domain.DefaultTarget.Target))
                {
                    if (context.Redirect(domain.DefaultTarget))
                    {
                        return;
                    }
                }
            }

            //Global default
            context.Redirect(SettingHost.ServiceSetting.DefaultTarget);
        }

        static bool TryRemapAlias(string alias, out string target)
        {
            if (SettingHost.ServiceSetting.Aliases.TryGetValue(alias, out var aliasTarget))
            {
                const int maxRedirectLimit = 16;
                var maxRedirect = maxRedirectLimit;
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

        static bool TryLocateRedirect(string address, IReadOnlyDictionary<string, RedirectTarget> records, out RedirectTarget target)
        {
            if (records.TryGetValue(address, out var redirectTarget))
            {
                if (redirectTarget.Target.StartsWith(">"))
                {
                    const int maxRedirectLimit = 16;
                    var maxRedirect = maxRedirectLimit;
                    address = redirectTarget.Target[1..];

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
                                address = redirectTarget.Target[1..];
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
                return $"{context.Request.Host.Host}:{context.Request.Host.Port}";
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

        private static string GetQueryTextParameter(this HttpRequest request, string parameter)
        {
            return request.Query[parameter].First();
        }

        private static string GetQueryOptionalTextParameter(this HttpRequest request, string parameter)
        {
            return request.Query[parameter].FirstOrDefault();
        }

        private static bool GetQueryBooleanParameter(this HttpRequest request, string parameter)
        {
            var value = request.Query[parameter].FirstOrDefault();
            return value switch
            {
                null => false,
                "1" => true,
                "0" => false,
                _ => value.Equals("true", StringComparison.OrdinalIgnoreCase)
            };
        }

        static string GetAccessKey(this HttpContext context)
        {
            return context.Request.Path.HasValue ? context.Request.Path.Value[1..] : null;
        }

        static bool Redirect(this HttpContext context, RedirectTarget target)
        {
            var text = target.Target;

            if (text.StartsWith("\""))
            {
                //customized
                return context.RedirectCustomized(text);
            }
            else if (text.StartsWith("<"))
            {
                //text mode
                return context.RedirectText(text);
            }
            else
            {
                context.RedirectNormal(text, target);
                return true;
            }
        }

        static bool RedirectCustomized(this HttpContext context, string text)
        {
            var second = text.IndexOf('\"', 1);
            if (second == -1)
                return false;

            if (text.Length == second + 1)
                return false;

            context.Response.ContentType = text[1..second];
            context.Response.WriteAsync(text[(second + 1)..]);
            return true;
        }

        static bool RedirectText(this HttpContext context, string text)
        {
            context.Response.ContentType = "text/plain;charset=UTF-8";
            if (text.Length > 1)
            {
                context.Response.WriteAsync(text[1..]);
                return true;
            }
            else
            {
                return false;
            }
        }

        static void RedirectNormal(this HttpContext context, string url, RedirectTarget target)
        {
            var query = context.Request.QueryString;
            if (query.HasValue && query.Value.Length > 0)
            {
                switch (target.QueryProcess)
                {
                    case RedirectQueryProcess.AppendDirectly:
                        url += query.Value;
                        break;
                    case RedirectQueryProcess.AppendRemovingLeadingQuestionMark when query.Value.StartsWith("?"):
                        url += "&" + query.Value[1..];
                        break;
                    case RedirectQueryProcess.AppendRemovingLeadingQuestionMark:
                        url += "&" + query.Value;
                        break;
                    //case RedirectQueryProcess.Ignored:
                    //    break;
                    //default:
                    //    throw new ArgumentOutOfRangeException();
                }
            }
            context.Response.Headers[HeaderNames.Location] = url;
            context.Response.StatusCode = target.Permanent ? StatusCodes.Status308PermanentRedirect : StatusCodes.Status307TemporaryRedirect;
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
