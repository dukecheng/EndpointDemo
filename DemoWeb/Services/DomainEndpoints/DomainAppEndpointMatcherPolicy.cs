using Microsoft.AspNetCore.Routing.Matching;

namespace DemoWeb.Services.DomainEndpoints;

/// <summary>
/// Match策略, 用于在路由匹配阶段筛选Endpoint.
/// </summary>
public class DomainAppEndpointMatcherPolicy : MatcherPolicy, IEndpointComparerPolicy, IEndpointSelectorPolicy
{
    public IComparer<Endpoint> Comparer => new DomainEndpointComparer();

    public override int Order => 0;

    /// <summary>
    /// 请求进来后第一个可以打断点的地方
    /// </summary>
    /// <param name="endpoints"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        _ = endpoints ?? throw new ArgumentNullException(nameof(endpoints));

        // When the node contains dynamic endpoints we can't make any assumptions.
        if (ContainsDynamicEndpoints(endpoints))
        {
            return true;
        }

        return endpoints.Any(e =>
        {
            var metadata = e.Metadata.GetMetadata<EndpointPointMatcherMetadata>();
            return metadata != null;
        });
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        _ = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
        _ = candidates ?? throw new ArgumentNullException(nameof(candidates));

        var headers = httpContext.Request.Headers;

        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates.IsValidCandidate(i))
            {
                continue;
            }

            var metadata = candidates[i].Endpoint.Metadata.GetMetadata<EndpointPointMatcherMetadata>();

            if (metadata is null)
            {
                continue;
            }

            var matched = metadata.IsValidForCurrentContext(httpContext);
            if (!matched)
            {
                candidates.SetValidity(i, false);
            }
        }

        return Task.CompletedTask;
    }
}
