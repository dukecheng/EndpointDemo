namespace DemoWeb.Services.DomainEndpoints;

public class PageCacheService
{
    private readonly IHostEnvironment _hostEnvironment;
    private const string PageCacheFolder = ".PageCaches";

    public PageCacheService(IHostEnvironment hostEnvironment)
    {
        this._hostEnvironment = hostEnvironment;
    }
    public string GenerageCacheFilename(string requestPath)
    {
        string fileName;

        // 检查请求路径是否以 / 结尾
        if (requestPath.EndsWith("/") || string.IsNullOrEmpty(requestPath))
        {
            // 构建目录路径并查找 index.html
            var directoryPath = Path.Combine(_hostEnvironment.ContentRootPath, PageCacheFolder, requestPath.Trim('/'));
            fileName = Path.Combine(directoryPath, "index.html");
        }
        else if (!requestPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            // 构建单个页面的文件路径
            fileName = Path.Combine(_hostEnvironment.ContentRootPath, PageCacheFolder, requestPath.Trim('/').Replace("/", "_") + ".html");
        }
        else
        {
            // 直接使用请求路径构建文件路径
            fileName = Path.Combine(_hostEnvironment.ContentRootPath, PageCacheFolder, requestPath.Trim('/').Replace("/", "_"));
        }

        return fileName;
    }

    public async Task WriteCacheFile(string requestPath, string content)
    {
        string fileName = GenerageCacheFilename(requestPath);
        // 确保目录存在
        Directory.CreateDirectory(Path.GetDirectoryName(fileName));
        // 将内容写入文件
        await File.WriteAllTextAsync(fileName, content);
    }
}
