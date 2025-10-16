namespace DemoWeb.Services.SupportedLocals;

/// <summary>
/// Supported localization language codes
/// </summary>
public enum SupportedLocalCodes
{
    /// <summary>English</summary>
    [SupportLocal(IsDefault = true)]
    En,
    /// <summary>Japanese</summary>
    [SupportLocal(IsDefault = false)]
    Jp,
    /// <summary>German</summary>
    [SupportLocal(IsDefault = false)]
    De,
    /// <summary>Spanish</summary>
    [SupportLocal(IsDefault = false)]
    Es,
    /// <summary>Chinese</summary>
    [SupportLocal(IsDefault = false, IsEnabled = false)]
    Zh
}