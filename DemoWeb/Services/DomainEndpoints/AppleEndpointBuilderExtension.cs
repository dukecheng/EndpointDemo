using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Reflection;

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

        var applicatoinPartsManager = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Mvc.ApplicationParts.ApplicationPartManager>();
        var routeData = context.GetRouteData();
        var controllerName = routeData.Values["controller"]?.ToString() ?? "Home";
        var actionName = routeData.Values["action"]?.ToString() ?? "Index";
        var controllerType = FindControllerType(controllerName);
        if (controllerType != null)
        {
            // 创建 ActionDescriptor
            var actionDescriptor = new ControllerActionDescriptor
            {
                // 设置控制器类型信息
                ControllerTypeInfo = controllerType.GetTypeInfo(),

                ControllerName = controllerName,

                // 设置操作名称
                ActionName = actionName,
                // 设置相关的属性
                MethodInfo = controllerType.GetMethod(actionName),
                // 可以根据需要设置其他属性
                Parameters = new List<ParameterDescriptor>(), // 这里可以添加参数描述符
                                                              // 其他属性...
            };

            // 设置 ActionContext
            var actionContext = new ActionContext(context, routeData, actionDescriptor);

            var controllerContext = new ControllerContext(actionContext);
            var controller = controllerActivator.Create(controllerContext) as Controller;

            // 调用指定的 Action 方法
            var actionMethod = controllerType.GetMethod(actionName);
            if (actionMethod != null)
            {
                var result = actionMethod.Invoke(controller, null) as IActionResult;

                // 处理 Action 结果
                if (result != null)
                {
                    // 确保结果是有效的
                    if (result is ViewResult viewResult)
                    {
                        // 使用系统自带的视图查找器查找视图
                        var viewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();

                        // 查找视图
                        var viewEngineResult = viewEngine.FindView(actionContext, actionName, isMainPage: false);
                        if (viewEngineResult.Success)
                        {
                            var view = viewEngineResult.View;
                            // 创建 ViewContext
                            using var writer = new StreamWriter(context.Response.Body);
                            // 确保流的位置是正确的
                            context.Response.Body.Position = 0;

                            var viewContext = new ViewContext(
                                actionContext,
                                view,
                                viewResult.ViewData,
                                viewResult.TempData,
                                writer,
                                new HtmlHelperOptions()
                            );

                            // 渲染视图
                            await view.RenderAsync(viewContext);
                        }
                        else
                        {
                            // 处理找不到视图的情况
                            context.Response.StatusCode = StatusCodes.Status404NotFound;
                            await context.Response.WriteAsync("视图未找到");
                        }
                    }
                    else
                    {
                        // 处理其他类型的结果
                    }
                }
            }
        }

        var host = context.Request.Host.Host;
        await context.Response.WriteAsync($"Hello from Apple App! Host: {host}");

        Type FindControllerType(string controllerName)
        {
            // 遍历 ApplicationPartManager 中的所有程序集，查找控制器类型
            foreach (AssemblyPart part in applicatoinPartsManager.ApplicationParts)
            {
                // 获取每个 ApplicationPart 的特征
                var assembly = part.Assembly;
                var controllerType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name.Equals($"{controllerName}Controller", StringComparison.OrdinalIgnoreCase));
                if (controllerType != null)
                {
                    return controllerType;
                }
            }
            return null;
        }
    }
}

//public class ViewRenderService : IViewRenderService
//{
//    private readonly IRazorViewEngine _razorViewEngine;
//    private readonly ITempDataProvider _tempDataProvider;
//    private readonly IServiceProvider _serviceProvider;

//    public ViewRenderService(IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
//    {
//        _razorViewEngine = razorViewEngine;
//        _tempDataProvider = tempDataProvider;
//        _serviceProvider = serviceProvider;
//    }

//    public async Task<string> RenderToStringAsync(string viewName, object model)
//    {
//        var actionContext = new ActionContext(
//            new DefaultHttpContext { RequestServices = _serviceProvider },
//            new RouteData(),
//            new ActionDescriptor()
//        );

//        using (var writer = new StringWriter())
//        {
//            var viewResult = _razorViewEngine.FindView(actionContext, viewName, false);
//            if (viewResult.View == null)
//            {
//                throw new ArgumentNullException($"View {viewName} not found.");
//            }

//            var viewContext = new ViewContext(
//                actionContext,
//                viewResult.View,
//                new ViewDataDictionary<object>(model),
//                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
//                writer,
//                new HtmlHelperOptions()
//            );

//            await viewResult.View.RenderAsync(viewContext);
//            return writer.ToString();
//        }
//    }
//}