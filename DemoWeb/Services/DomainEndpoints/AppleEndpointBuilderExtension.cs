using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace DemoWeb.Services.DomainEndpoints;

public static class AppleEndpointBuilderExtension
{
    /// <summary>
    /// 1. Create a middleware pipeline that handles requests for the Apple app.
    /// 2. Register the pipeline with a custom endpoint data source that routes requests based on domain.
    /// </summary>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static void MapAppleAppEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // 从这里看起来, 每次Map都会创建一个新的RequestDelegate
        RequestDelegate app = CreateReuestPipeline(endpoints);

        // 这里创建了DataSource, 并且把RequestDelegate传给DataSource
        // 接着把DataSource添加到endpoints.DataSources中
        var dataSource = endpoints.DataSources.OfType<DomainAppEndpointDataSource>().FirstOrDefault();
        if (dataSource is null)
        {
            dataSource = endpoints.ServiceProvider.GetRequiredService<DomainAppEndpointDataSource>();
            dataSource.RequestProcessPipeline = app;
            endpoints.DataSources.Add(dataSource);

            // Config validation is async but startup is sync. We want this to block so that A) any validation errors can prevent
            // the app from starting, and B) so that all the config is ready before the server starts accepting requests.
            // Reloads will be async.
            dataSource.InitialLoadAsync().GetAwaiter().GetResult();
        }
        //return dataSource?.DefaultBuilder ?? throw new ArgumentNullException(nameof(dataSource));
    }

    private static RequestDelegate CreateReuestPipeline(IEndpointRouteBuilder endpoints)
    {
        var appBuilder = endpoints.CreateApplicationBuilder();

        appBuilder.Use(async (httpContext, _next) =>
        {
            var domainAppFeature = httpContext.Features.Get<IDomainAppeature>() ??
                                   throw new InvalidOperationException("DomainAppFeature is missing.");
            var serviceProvider = httpContext.RequestServices;
            var domainApps = serviceProvider.GetRequiredService<IOptions<DomainApps>>().Value;

            var host = httpContext.Request.Host.Value;
            var domainApp =
                domainApps.FirstOrDefault(x => string.Equals(x.Host, host, StringComparison.OrdinalIgnoreCase)) ??
                throw new InvalidOperationException($"DomainApp {host} does not exist.");
            domainAppFeature.DomainApp = domainApp;
            httpContext.Features.Set(domainAppFeature);

            await _next(httpContext);
        });

        // Add middleware to the pipeline
        appBuilder.Run(FinalRequestProcess);

        // Build the application pipeline and Set to Endpoint Factory
        var app = appBuilder.Build();
        return app;
    }

    public static async Task FinalRequestProcess(HttpContext context)
    {
        // 获取 IControllerActivator 实例
        var serviceProvider = context.RequestServices;
        var controllerActivator = serviceProvider.GetRequiredService<IControllerActivator>();
        var hostEnvironment = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var pageCacheService = serviceProvider.GetRequiredService<PageCacheService>();

        var applicatoinPartsManager = serviceProvider
            .GetRequiredService<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager>();

        var routeData = context.GetRouteData();
        var controllerName = routeData.Values["controller"]?.ToString();
        var actionName = routeData.Values["action"]?.ToString();

        if (string.IsNullOrWhiteSpace(controllerName) || string.IsNullOrWhiteSpace(actionName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Controller or Action not specified");
            return;
        }

        var controllerType = FindControllerType(controllerName);
        if (controllerType != null)
        {
            // 创建 ActionDescriptor
            MethodInfo methodInfo = controllerType.GetMethods()
                .FirstOrDefault(m => string.Equals(m.Name, actionName, StringComparison.OrdinalIgnoreCase));
            var actionDescriptor = new ControllerActionDescriptor
            {
                // 设置控制器类型信息
                ControllerTypeInfo = controllerType.GetTypeInfo(),

                ControllerName = controllerName,

                // 设置操作名称
                ActionName = actionName,
                // 设置相关的属性
                MethodInfo = methodInfo,
                // 可以根据需要设置其他属性
                Parameters = new List<ParameterDescriptor>(), // 这里可以添加参数描述符
                // 其他属性...
            };

            // 设置 ActionContext
            var actionContext = new ActionContext(context, routeData, actionDescriptor);

            var controllerContext = new ControllerContext(actionContext);
            var controller = controllerActivator.Create(controllerContext) as Controller;

            // 调用指定的 Action 方法
            var actionMethod = actionDescriptor.MethodInfo;
            if (actionMethod != null)
            {
                var result = actionMethod.Invoke(controller, null) as IActionResult;

                // 处理 Action 结果
                if (result != null && result is ViewResult viewResult)
                {
                    if (viewResult.ViewData == null)
                        viewResult.ViewData = new ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
                    if (viewResult.TempData == null)
                        viewResult.TempData = serviceProvider.GetRequiredService<ITempDataDictionaryFactory>()
                            .GetTempData(context);

                    // 使用系统自带的视图查找器查找视图
                    //var viewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
                    var razorPageFactoryProvider = serviceProvider.GetRequiredService<IRazorPageFactoryProvider>();
                    var razorPageActivator = serviceProvider.GetRequiredService<IRazorPageActivator>();
                    var htmlEncoder = serviceProvider.GetRequiredService<HtmlEncoder>();
                    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                    var diagnosticListener = serviceProvider.GetRequiredService<DiagnosticListener>();
                    var defaultViewEngineOption =
                        serviceProvider.GetRequiredService<IOptions<RazorViewEngineOptions>>();

                    var domainApp = context.Features.Get<IDomainAppeature>()?.DomainApp ??
                                    throw new InvalidOperationException("DomainAppFeature is missing.");

                    var veOptions = new RazorViewEngineOptions();
                    var viewBase = $"/SiteViews/{domainApp.SmallerIdentifier}";
                    veOptions.ViewLocationFormats.Add(viewBase + "/Views/{1}/{0}.cshtml");
                    veOptions.ViewLocationFormats.Add(viewBase + "/Views/Shared/{0}.cshtml");
                    veOptions.AreaViewLocationFormats.Add(viewBase + "/Areas/{2}/Views/{1}/{0}.cshtml");
                    veOptions.AreaViewLocationFormats.Add(viewBase + "/Areas/{2}/Views/Shared/{0}.cshtml");
                    var options = Options.Create(veOptions);
                    var viewEngine = new ViewEngins.DomainAppRazorViewEngine(razorPageFactoryProvider,
                        razorPageActivator, htmlEncoder, options, loggerFactory, diagnosticListener);

                    // 查找视图
                    var viewEngineResult = viewEngine.FindView(actionContext, actionName, isMainPage: false);
                    if (viewEngineResult.Success)
                    {
                        var view = viewEngineResult.View as Microsoft.AspNetCore.Mvc.Razor.RazorView;
                        var viewData = new ViewDataDictionary(viewResult.ViewData);

                        // 创建 ViewContext
                        await using var writer = new StringWriter();
                        // 读取并设置Layout
                        // var layout = viewData["Layout"] ?? "_Layout"; // 你可以检查ViewData中是否已有(Layout)或者设置为默认布局
                        //
                        // viewData["Layout"] = layout;
                        //
                        // view.RazorPage.Layout = layout.ToString();

                        var viewContext = new ViewContext(
                            actionContext,
                            view,
                            viewData,
                            viewResult.TempData,
                            writer,
                            new HtmlHelperOptions()
                        );


                        // 渲染视图
                        await view.RenderAsync(viewContext);
                        var content = writer.ToString();
                        await pageCacheService.WriteCacheFile(context.Request.Path, content);
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync(content);
                    }
                    else
                    {
                        // 处理找不到视图的情况
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        await context.Response.WriteAsync("View not found");
                    }
                }
            }
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Controller not found");
        }

        return;

        Type FindControllerType(string controllerName)
        {
            // 遍历 ApplicationPartManager 中的所有程序集，查找控制器类型
            foreach (AssemblyPart part in applicatoinPartsManager.ApplicationParts.OfType<AssemblyPart>())
            {
                // 获取每个 ApplicationPart 的特征
                var assembly = part.Assembly;
                var controllerType = assembly.GetTypes()
                    .FirstOrDefault(t =>
                        t.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase));
                if (controllerType != null)
                {
                    return controllerType;
                }
            }

            return null;
        }
    }
}