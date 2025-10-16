using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace DemoWeb.Services.DomainEndpoints;

public class DomainAppEndpointDataSource : EndpointDataSource
{
    private List<Endpoint>? _endpoints = null;
    private readonly object _syncRoot = new();
    private readonly ConcurrentDictionary<string, RouteState> _routesStates = new(StringComparer.OrdinalIgnoreCase);
    //private readonly List<Action<EndpointBuilder>> _conventions;

    public RequestDelegate? RequestProcessPipeline { get; set; } = null;

    //public DomainAppEndpointConventionBuilder DefaultBuilder { get; }//ControllerActionEndpointConventionBuilder

    private CancellationTokenSource _endpointsChangeSource = new();
    private DomainApps _domainApps;
    private IChangeToken _endpointsChangeToken;
    public override IChangeToken GetChangeToken() => Volatile.Read(ref _endpointsChangeToken);

    public DomainAppEndpointDataSource(IOptions<DomainApps> domainAppsOption)
    {
        _domainApps = domainAppsOption.Value;
        _endpointsChangeToken = new CancellationChangeToken(_endpointsChangeSource.Token);
        //_conventions = new List<Action<EndpointBuilder>>();
        //DefaultBuilder = new DomainAppEndpointConventionBuilder(_conventions);
    }

    public override IReadOnlyList<Endpoint> Endpoints
    {
        get
        {
            if (_endpoints is null)
            {
                lock (_syncRoot)
                {
                    if (_endpoints is null)
                    {
                        CreateEndpoints();
                    }
                }
            }

            return _endpoints;
        }
    }

    [MemberNotNull(nameof(_endpoints))]
    private void CreateEndpoints()
    {
        /*
         每个Map可以创建多个Endpoint，每个Endpoint有自己的路由模式
         */
        var endpoints = new List<Endpoint>();
        foreach (var existingRoute in _routesStates)
        {
            var endpoint = existingRoute.Value.CachedEndpoint;
            if (endpoint is null)
            {
                //endpoint = _managedApiEndpointFactory.CreateManagedApiEndpoint($"managedapi-{existingRoute.Value.RouteId}", 0, _conventions);
                endpoint = CreateEndpoint(existingRoute.Value, 0, null, RequestProcessPipeline);
                existingRoute.Value.CachedEndpoint = endpoint;
            }

            endpoints.Add(endpoint);
        }

        UpdateEndpoints(endpoints);


        Endpoint CreateEndpoint(RouteState routeState, int order, IList<Action<EndpointBuilder>>? conventions,
            RequestDelegate? requestPipeline)
        {
            // conventions的作用是针对EndpointBuilder进行一些定制化的操作, 以达到所有的Endpoint都具有某些共性
            var builder = new RouteEndpointBuilder(
                requestDelegate: requestPipeline ??
                                 throw new InvalidOperationException("The pipeline hasn't been provided yet."),
                RoutePatternFactory.Parse(routeState.RoutePattern),
                order)
            {
                DisplayName = routeState.RoutePattern
            };

            builder.Metadata.Add(new EndpointPointMatcherMetadata() { Host = routeState.Host });
            if (conventions is not null)
            {
                foreach (var convention in conventions)
                {
                    convention(builder);
                }
            }

            return builder.Build();
        }
    }

    /// <summary>
    /// Applies a new set of ASP .NET Core endpoints. Changes take effect immediately.
    /// </summary>
    /// <param name="endpoints">New endpoints to apply.</param>
    [MemberNotNull(nameof(_endpoints))]
    private void UpdateEndpoints(List<Endpoint> endpoints)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        lock (_syncRoot)
        {
            // These steps are done in a specific order to ensure callers always see a consistent state.

            // Step 1 - capture old token
            var oldCancellationTokenSource = _endpointsChangeSource;

            // Step 2 - update endpoints
            Volatile.Write(ref _endpoints, endpoints);

            // Step 3 - create new change token
            _endpointsChangeSource = new CancellationTokenSource();
            Volatile.Write(ref _endpointsChangeToken, new CancellationChangeToken(_endpointsChangeSource.Token));

            // Step 4 - trigger old token
            oldCancellationTokenSource?.Cancel();
        }
    }

    internal async Task<EndpointDataSource> InitialLoadAsync()
    {
        try
        {
            //using var scope = AgileLabContexts.Context.CreateScopeWithWorkContext();
            //var apiDescription = scope.WorkContext.ServiceProvider.GetRequiredService<IApiDescriptionService>();

            //var managedApiServices = apiDescription.GetApiControllers();

            //foreach (var apiService in managedApiServices)
            //{
            //    var newState = new ApiControllerRouteState()
            //    {
            //        RouteId = $"route-{apiService.ServiceType.Name}",
            //        Metadata = new Dictionary<string, object> { { "RawModel", apiService } }
            //    };
            //    _routes.TryAdd(newState.RouteId, newState);
            //}
            //await Task.CompletedTask;

            /*
             这里动态的获取路由信息，然后添加到_routesStates中

             */

            await Task.CompletedTask;
            foreach (var item in _domainApps)
            {
                _routesStates.TryAdd("DomainAppResource", new RouteState { Host = item.Host, RoutePattern = "/{lang:SupportedLocals}/resource/{level1Category}/{*slug}" });
                _routesStates.TryAdd("DomainAppDefault", new RouteState { Host = item.Host, RoutePattern = "/{lang:SupportedLocals}/{controller=Home}/{action=Index}" });
            }

        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Unable to load or apply the proxy configuration.", ex);
        }

        return this;
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