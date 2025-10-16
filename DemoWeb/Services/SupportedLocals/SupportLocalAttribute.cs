namespace DemoWeb.Services.SupportedLocals;

public class SupportLocalAttribute : Attribute
{
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}