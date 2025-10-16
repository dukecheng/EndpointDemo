using AgileLabs.AspNet.Mvcs;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Niusys.Cms.SiteClient;
using Niusys.Cms.SiteClient.Models;

namespace DemoWeb.WebFrameworks;

public abstract class BaseRazorPage<T> : SeoRazorPage<T>
{
    const string BreadcrumbStoreKey = "BreadcrumbStore";

    public string Lang => Context.Items["Lang"]?.ToString() ?? "en";

    public string H1Title
    {
        get
        {
            var breadcrumbStore = GetValueFromViewDataStore<PageBreadcrumb>(BreadcrumbStoreKey);
            if (!string.IsNullOrWhiteSpace(breadcrumbStore.H1Title))
            {
                return breadcrumbStore.H1Title;
            }

            var h1Title = ViewBag.H1Title?.ToString();
            if (string.IsNullOrWhiteSpace(h1Title))
            {
                h1Title = string.Empty;
            }
            return h1Title;
        }
        set
        {
            var breadcrumbStore = GetValueFromViewDataStore<PageBreadcrumb>(BreadcrumbStoreKey);
            breadcrumbStore.H1Title = value;
        }
    }
    /// <summary>
    /// 隐藏CanonicalUrl, 默认不隐藏
    /// </summary>
    public bool ShowCanonicalUrl { get; set; } = true;

    public string AddFileVersionToPath(string path)
    {
        return Context
            .RequestServices
            .GetRequiredService<IFileVersionProvider>()
            .AddFileVersionToPath(Context.Request.PathBase, path);
    }

    public string BreadcrumbBgImg
    {
        get
        {
            var breadcrumbStore = GetValueFromViewDataStore<PageBreadcrumb>(BreadcrumbStoreKey);
            return breadcrumbStore.BackgroundImage;
        }
        set
        {
            var breadcrumbStore = GetValueFromViewDataStore<PageBreadcrumb>(BreadcrumbStoreKey);
            breadcrumbStore.BackgroundImage = value;
        }
    }
    public IReadOnlyList<BreadcrumbLinkItem> BreadcrumbList
    {
        get
        {
            var breadcrumbStore = GetValueFromViewDataStore<PageBreadcrumb>(BreadcrumbStoreKey);
            var breadcrumbList = breadcrumbStore.Items;
            breadcrumbList.Reverse();
            return breadcrumbList.AsReadOnly();
        }
    }

    //public IGmWorkContext WorkContext => Context.RequestServices.GetService<IGmWorkContext>();

    public bool IsIndex
    {
        get => GetValueFromViewDataStore<bool>(nameof(IsIndex), true);
        set => SetViewDataStore(nameof(IsIndex), value);
    }

    public void PushBreadcrumbLink(string text, string link = "", string linkTitle = "", string isActive = "")
    {

        var breadcrumbStore = GetValueFromViewDataStore<PageBreadcrumb>(BreadcrumbStoreKey);
        breadcrumbStore.Items.Add(new BreadcrumbLinkItem { Text = text, Link = link, LinkTitle = linkTitle, IsActive = isActive });
    }

    private TStoreValue GetValueFromViewDataStore<TStoreValue>(string storeKey, TStoreValue defaultValue = default)
        where TStoreValue : new()
    {
        if (!this.ViewData.TryGetValue(storeKey, out var breadcrumbStoreInstance))
        {
            breadcrumbStoreInstance = defaultValue ?? new TStoreValue();
            this.ViewData.TryAdd(storeKey, breadcrumbStoreInstance);
        }

        return (TStoreValue)breadcrumbStoreInstance;
    }

    private void SetViewDataStore<TStoreValue>(string storeKey, TStoreValue value)
    {
        if (!this.ViewData.TryGetValue(storeKey, out var breadcrumbStoreInstance))
        {
            breadcrumbStoreInstance = value;
            this.ViewData.TryAdd(storeKey, breadcrumbStoreInstance);
        }

        this.ViewData[storeKey] = value;
    }

    public async Task<List<NameValueItem>> GetResourceMenuList()
    {
        var list = new List<NameValueItem>();
        var client = Context.RequestServices.GetRequiredService<CmsWebsiteApiClient>();
        var result = await client.CategorySearchAsync(new CategorySearchRequest
        {
            ParentCategoryId = 4,
            Tags = new[] { "RESOURCE" },
            PageIndex = 1,
            PageSize = int.MaxValue
        });

        foreach (var item in result.Records)
        {
            list.Add(new NameValueItem { Name = item.Name, Slug = $"~/{item.Slug}" });
        }
        return list;
    }

    //public IDetectionService DetectionService => Context.RequestServices.GetRequiredService<IDetectionService>();

    //public bool IsDesktopMode => DetectionService.Device.Type == Device.Desktop;
    //public bool IsTabletMode => DetectionService.Device.Type == Device.Tablet;
    //public bool IsMobileMode => DetectionService.Device.Type == Device.Mobile;
}