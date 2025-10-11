namespace DemoWeb.Services.DomainEndpoints;

public class RouteState
{
    public string RoutePattern { get; set; }
    public Endpoint? CachedEndpoint { get; set; }
}