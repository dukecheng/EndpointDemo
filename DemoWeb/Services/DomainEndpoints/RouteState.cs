namespace DemoWeb.Services.DomainEndpoints;

public class RouteState
{
    public required string RoutePattern { get; set; }
    public Endpoint? CachedEndpoint { get; set; }
}