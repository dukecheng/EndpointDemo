namespace DemoWeb.WebFrameworks;

public class PageBreadcrumb
{
    public string H1Title { get; set; }
    public string BackgroundImage { get; set; }
    public List<BreadcrumbLinkItem> Items { get; set; } = new();
}