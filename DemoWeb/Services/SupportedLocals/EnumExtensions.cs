using System.ComponentModel;

namespace DemoWeb.Services.SupportedLocals;

public static class EnumExtensions
{
    public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
    {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString());
        if (memberInfo != null && memberInfo.Length > 0)
        {
            var attrs = memberInfo[0].GetCustomAttributes(typeof(TAttribute), false);
            if (attrs != null && attrs.Length > 0)
            {
                return (TAttribute)attrs[0];
            }
        }
        return null;
    }

    public static string GetDescription(this Enum value)
    {
        var attr = value.GetAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }
}