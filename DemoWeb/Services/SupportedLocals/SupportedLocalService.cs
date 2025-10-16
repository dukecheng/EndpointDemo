namespace DemoWeb.Services.SupportedLocals;

public class SupportedLocalService
{
    public static List<SupportedLocalCodes> GetAllEnabledLocals()
    {
        var enabledLocals = new List<SupportedLocalCodes>();
        foreach (SupportedLocalCodes code in Enum.GetValues(typeof(SupportedLocalCodes)))
        {
            var attribute = code.GetAttribute<SupportLocalAttribute>();
            if (attribute.IsEnabled)
            {
                if (attribute.IsDefault)
                {
                    enabledLocals.Insert(0, code);
                }
                else
                {
                    enabledLocals.Add(code);
                }
            }
        }
        return enabledLocals;
    }
}