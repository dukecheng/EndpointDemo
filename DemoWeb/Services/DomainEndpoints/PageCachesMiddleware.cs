using System.Linq;

namespace DemoWeb.Services.DomainEndpoints;

public class PageCachesMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _hostEnvironment;

    public PageCachesMiddleware(RequestDelegate next, IHostEnvironment hostEnvironment)
    {
        _next = next;
        _hostEnvironment = hostEnvironment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var pathSegments = requestPath.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(0);
        var lastSegment = pathSegments.LastOrDefault();

        // 检查查询字符串中是否包含 RefreshCache=true
        if (context.Request.Query.ContainsKey("RefreshCache") && bool.TryParse(context.Request.Query["RefreshCache"], out var refreshCache) && refreshCache)
        {
            // 跳过静态处理，继续执行管道中的下一个中间件
            RemoveHtmlExtension(context, pathSegments, lastSegment);
            await _next(context);
            return;
        }


        if (pathSegments.Count == 0)
        {
            context.Response.Headers.Location = "/index.html";
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            return;
        }
        else if (lastSegment != null && !lastSegment.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers.Location = "/" + string.Join("/", pathSegments.Take(pathSegments.Count).Select(x => x.ToLower())) + ".html";
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
                var autoGenerateCache = true; // 这里可以根据需要设置为 true 或 false
                if (autoGenerateCache)
                {
                    // 跳过静态处理，继续执行管道中的下一个中间件
                    RemoveHtmlExtension(context, pathSegments, lastSegment);
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
            Console.Error.WriteLine($"Error reading cache file: {ex.Message}");
        }
        catch (Exception ex)
        {
            // 处理其他潜在异常
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
        }

        // 如果文件不存在，调用下一个中间件
        await _next(context);

        static void RemoveHtmlExtension(HttpContext context, List<string> pathSegments, string? lastSegment)
        {
            if (lastSegment?.EndsWith(".html") ?? false)
            {
                if (lastSegment.Equals("index.html"))
                    context.Request.Path = "/" + string.Join("/", pathSegments.Take(pathSegments.Count - 1).Select(x => x.ToLower()));
                else
                {
                    var newSegments = new List<string>(pathSegments.Count + 1);
                    newSegments.AddRange(pathSegments.Take(pathSegments.Count - 1));
                    newSegments.Add(lastSegment.Substring(0, lastSegment.Length - 5));
                    context.Request.Path = "/" + string.Join("/", newSegments.Select(x => x.ToLower()));
                }
            }
        }
    }
}
