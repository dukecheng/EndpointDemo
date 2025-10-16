using Microsoft.AspNetCore.Routing.Matching;

namespace DemoWeb.Services.DomainEndpoints;

internal class DomainEndpointComparer : EndpointMetadataComparer<EndpointPointMatcherMetadata>
{
    protected override int CompareMetadata(EndpointPointMatcherMetadata x, EndpointPointMatcherMetadata y) => 0;
}
public interface IDomainAppeature
{
    GenerateMode GenerateMode { get; set; }
    DomainApp DomainApp { get; set; }
}
public class DomainAppFeature : IDomainAppeature
{
    public GenerateMode GenerateMode { get; set; }
    public DomainApp DomainApp { get; set; }
}
public enum GenerateMode
{
    None = 0,
    ForceGenerating,
    AutoGenerating,
    BatchGenerating,
}