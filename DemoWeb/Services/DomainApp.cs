namespace DemoWeb.Services
{
    public class DomainApp
    {
        public string Name { get; set; }
        public string Identifier { get; set; }
        public string SmallerIdentifier { get; set; }
        public string Host { get; set; }
    }

    public class DomainApps : List<DomainApp>
    { }
}
