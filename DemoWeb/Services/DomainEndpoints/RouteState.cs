namespace DemoWeb.Services.DomainEndpoints;

public class RouteState
{
    public string Host { get; internal set; }
    public required string RoutePattern { get; set; }
    public Endpoint? CachedEndpoint { get; set; }
}