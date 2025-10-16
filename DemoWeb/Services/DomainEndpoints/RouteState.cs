namespace DemoWeb.Services.DomainEndpoints;

public class RouteState
{
    public required string Host { get; set; }
    public required string RoutePattern { get; set; }
    public Endpoint? CachedEndpoint { get; set; }
    public required DomainApp DomainApp { get; set; }
}