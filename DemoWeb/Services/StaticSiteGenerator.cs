using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DemoWeb.Services;

public class StaticSiteGenerator
{
    private readonly IViewRenderService _viewRenderService;

    public StaticSiteGenerator(IViewRenderService viewRenderService)
    {
        _viewRenderService = viewRenderService;
    }

    public async Task GenerateStaticPage(string pageName, object model, string outputPath)
    {
        var htmlContent = await _viewRenderService.RenderToStringAsync(pageName, model);
        await File.WriteAllTextAsync(outputPath, htmlContent);
    }
}

public interface IViewRenderService
{
    Task<string> RenderToStringAsync(string viewName, object model);
}

public class ViewRenderService : IViewRenderService
{
    public Task<string> RenderToStringAsync(string viewName, object model)
    {
        throw new NotImplementedException();
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