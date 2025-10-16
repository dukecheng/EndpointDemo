namespace DemoWeb.Services.SupportedLocals;

public class LangRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string parameterName,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));
        if (values.TryGetValue(parameterName, out var value) && value is string lang)
        {
            if (Enum.TryParse<SupportedLocalCodes>(lang, true, out var supportLocalValue))
            {
                var attribute = supportLocalValue.GetAttribute<SupportLocalAttribute>();
                return attribute.IsEnabled;
            }
        }
        return false;
    }
}