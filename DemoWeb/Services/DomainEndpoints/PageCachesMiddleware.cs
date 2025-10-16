using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Linq;

namespace DemoWeb.Services.DomainEndpoints;

public class PageCachesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly DomainApps _domainAppsOption;
    private bool AutoGenerateCache = false; // 这里可以根据需要设置为 true 或 false

    public PageCachesMiddleware(RequestDelegate next, IHostEnvironment hostEnvironment,
        IOptions<DomainApps> domainAppsOption)
    {
        _next = next;
        _hostEnvironment = hostEnvironment;
        _domainAppsOption = domainAppsOption.Value;
        AutoGenerateCache = true;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 只有GET的请求走Cache
        if (!context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 只针对DomainApps
        if (!_domainAppsOption.Any(x => x.Host.Equals(context.Request.Host.Value, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var langSegment = context.Items["Lang"]?.ToString() ?? string.Empty;
        var isLangExist = !string.IsNullOrEmpty(langSegment);
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var fullPathSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        List<string> pathSegments = null;
        if (isLangExist)
        {
            pathSegments = fullPathSegments.Skip(1).ToList();
        }
        else
        {
            pathSegments = fullPathSegments;
        }

        var lastSegment = pathSegments.LastOrDefault();

        // 只处理mvc请求以及.html的请求
        if (!CanProcess(lastSegment))
        {
            await _next(context);
            return;
        }

        // 检查查询字符串中是否包含 RefreshCache=true
        // 对于mvc地址也支持直接生成
        if (context.Request.Query.ContainsKey("RefreshCache") &&
            bool.TryParse(context.Request.Query["RefreshCache"], out var refreshCache) && refreshCache)
        {
            // 跳过静态处理，继续执行管道中的下一个中间件
            RemoveHtmlExtension(context, langSegment, pathSegments);
            SetDomainAppFeature(context, GenerateMode.ForceGenerating);
            await _next(context);
            return;
        }

        // 规范化格式，没有带.html的mvc原始请求自动带上.html后进行301
        if (pathSegments.Count == 0)
        {
            context.Response.Headers.Location = $"/{langSegment}/index.html";
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            return;
        }
        else if (lastSegment != null && !lastSegment.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.Location = $"/{langSegment}/" +
                                                string.Join("/",
                                                    pathSegments.Take(pathSegments.Count).Select(x => x.ToLower())) +
                                                ".html";
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            return;
        }

        var serviceProvider = context.RequestServices;
        var pageCacheService = serviceProvider.GetRequiredService<PageCacheService>();
        string fileName = pageCacheService.GenerageCacheFilename(context.Request.Path);

        try
        {
            // 检查文件是否存在
            if (File.Exists(fileName))
            {
                var content = await File.ReadAllTextAsync(fileName);
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(content);
                return; // 结束请求处理
            }
            else
            {
                if (AutoGenerateCache)
                {
                    // 跳过静态处理，继续执行管道中的下一个中间件
                    RemoveHtmlExtension(context, langSegment, pathSegments);
                    SetDomainAppFeature(context, GenerateMode.AutoGenerating);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }
        }
        catch (IOException ex)
        {
            // 记录错误信息
            await Console.Error.WriteLineAsync($"Error reading cache file: {ex.Message}");
        }
        catch (Exception ex)
        {
            // 处理其他潜在异常
            await Console.Error.WriteLineAsync($"Unexpected error: {ex.Message}");
        }

        // 如果文件不存在，调用下一个中间件
        await _next(context);

        static void RemoveHtmlExtension(HttpContext context, string langSegment, List<string> pathSegments)
        {
            var lastSegment = pathSegments.LastOrDefault();
            if (lastSegment?.EndsWith(".html") ?? false)
            {
                var urlSegments = new List<string>(pathSegments.Count + 2) { langSegment };
                urlSegments.AddRange(pathSegments.Take(pathSegments.Count - 1));

                if (lastSegment.Equals("index.html"))
                    context.Request.Path = "/" + string.Join("/", urlSegments.Select(x => x.ToLower()));
                else
                {
                    urlSegments.Add(lastSegment.Substring(0, lastSegment.Length - 5));
                    context.Request.Path = "/" + string.Join("/", urlSegments.Select(x => x.ToLower()));
                }
            }
        }

        static void SetDomainAppFeature(HttpContext context, GenerateMode generateMode)
        {
            var feature = context.Features.Get<IDomainAppeature>();
            if (feature == null)
            {
                feature = new DomainAppFeature();
            }

            feature.GenerateMode = generateMode;
            context.Features.Set(feature);
        }

        // 1. 请求的地址为空
        // 2. 请求的地址没有后缀
        // 3. 请求的地址有后缀，但是后缀是.html
        static bool CanProcess(string? lastSegment)
        {
            if (string.IsNullOrEmpty(lastSegment))
            {
                return true;
            }
            else if (lastSegment.IndexOf('.') == -1)
            {
                return true;
            }
            else if (lastSegment.IndexOf(".") > 0 && lastSegment.EndsWith(".html"))
            {
                return true;
            }

            return false;
        }
    }
}