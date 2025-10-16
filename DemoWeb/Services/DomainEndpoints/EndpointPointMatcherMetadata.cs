namespace DemoWeb.Services.DomainEndpoints;

public class EndpointPointMatcherMetadata
{
    public string Host { get; internal set; }

    // 这里填写Matcher的一些元数据信息, 在Matcher要检查这些信息是否匹配
    public bool IsValidForCurrentContext(HttpContext context)
    {
        var validResult = true;
        if (context == null)
        {
            validResult = false;
        }

        var host = context.Request.Host.Value;
        if (string.IsNullOrEmpty(host))
        {
            validResult = false;
        }

        if (!host.Equals(Host))
        {
            validResult = false;
        }

        return validResult;
    }
}
