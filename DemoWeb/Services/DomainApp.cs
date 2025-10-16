namespace DemoWeb.Services
{
    public class DomainApp
    {
        public required string Name { get; set; }
        public required string Identifier { get; set; }
        public required string SmallerIdentifier { get; set; }
        public required string Host { get; set; }
    }

    public class DomainApps : List<DomainApp>
    { }
}
