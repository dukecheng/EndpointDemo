namespace DemoWeb.Services.DomainEndpoints;

public class EndpointPointMatcherMetadata
{
    // 这里填写Matcher的一些元数据信息, 在Matcher要检查这些信息是否匹配
    public bool IsValidForCurrentContext(HttpContext context)
    {
        return true;
    }
}
