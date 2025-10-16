using System.Globalization;
using AgileLabs;
using DemoWeb.Services.SupportedLocals;

namespace DemoWeb.Services;

/// <summary>
/// 
/// </summary>
public class RequestLocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _supportedCultures;

    public RequestLocalizationMiddleware(RequestDelegate next)
    {
        _next = next;
        _supportedCultures =
            [.. SupportedLocalService.GetAllEnabledLocals().Select(x => x.ToString().ToLower()).ToList()];
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value;
        // 检查请求路径、静态文件、请求方法
        if (!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (context.Request.IsStaticResource())
        {
            await _next(context);
            return;
        }

        var segments = path?.Split('/', StringSplitOptions.RemoveEmptyEntries);

        string? lang = null;
        if (segments is { Length: > 0 } && segments[0].Length == 2)
        {
            var langSegments = segments[0].ToLower();
            if (_supportedCultures.Contains(langSegments))
            {
                // 规范化小写
                if (!string.Equals(langSegments, segments[0]))
                {
                    string[] newPath = [langSegments, .. segments.Skip(1)];

                    var newUrl = $"/{newPath.JoinStrings("/")}" + context.Request.QueryString;
                    context.Response.Redirect(newUrl, permanent: true); // permanent: true 表示 301
                    return;
                }

                lang = segments[0];
            }
        }

        if (lang == null)
        {
            // 没有语言编码，可能是首页
            lang = "en"; // 默认
            // 从Accept-Language获取首选
            var acceptLang = context.Request.Headers["Accept-Language"].ToString();
            if (!string.IsNullOrEmpty(acceptLang))
            {
                // 解析首选语言
                var browserLang = acceptLang.Split(',').Select(l => l.Split(';')[0].Trim()).FirstOrDefault();
                if (!string.IsNullOrEmpty(browserLang))
                {
                    // 只取前2位
                    var shortLang = browserLang.Length >= 2 ? browserLang.Substring(0, 2) : browserLang;
                    if (_supportedCultures.Contains(shortLang))
                    {
                        //context.Items["RecommendedLang"] = shortLang;
                        lang = shortLang;
                    }
                    else
                    {
                        var defaultLang = _supportedCultures.First();
                        //context.Items["RecommendedLang"] = defaultLang;
                        lang = defaultLang;
                    }
                }
            }

            string[] newPath = [lang, .. segments];
            var newUrl = $"/{newPath.JoinStrings("/")}" + context.Request.QueryString;
            context.Response.Redirect(newUrl, permanent: true); // permanent: true 表示 301
            return;
        }

        context.Items["Lang"] = lang;

        // 设置Culture
        var culture = new CultureInfo(lang);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await _next(context);
    }
}