using Microsoft.AspNetCore.Routing.Matching;

namespace DemoWeb.Services.DomainEndpoints;

internal class DomainEndpointComparer : EndpointMetadataComparer<EndpointPointMatcherMetadata>
{
    protected override int CompareMetadata(EndpointPointMatcherMetadata x, EndpointPointMatcherMetadata y) => 0;
}
