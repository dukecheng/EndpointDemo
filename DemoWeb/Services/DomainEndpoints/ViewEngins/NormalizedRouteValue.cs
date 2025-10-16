using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Primitives;

namespace DemoWeb.Services.DomainEndpoints.ViewEngins;

internal static class RazorFileHierarchy
{
    private const string ViewStartFileName = "_ViewStart.cshtml";

    public static IEnumerable<string> GetViewStartPaths(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (path[0] != '/')
        {
            throw new ArgumentException("RazorProject_PathMustStartWithForwardSlash", nameof(path));
        }

        if (path.Length == 1)
        {
            yield break;
        }

        var builder = new StringBuilder(path);
        var maxIterations = 255;
        var index = path.Length;
        while (maxIterations-- > 0 && index > 1 && (index = path.LastIndexOf('/', index - 1)) != -1)
        {
            builder.Length = index + 1;
            builder.Append(ViewStartFileName);

            var itemPath = builder.ToString();
            yield return itemPath;
        }
    }
}

/// <summary>
/// Key for entries in <see cref="RazorViewEngine.ViewLookupCache"/>.
/// </summary>
internal readonly struct ViewLocationCacheKey : IEquatable<ViewLocationCacheKey>
{
    /// <summary>
    /// Initializes a new instance of <see cref="ViewLocationCacheKey"/>.
    /// </summary>
    /// <param name="viewName">The view name or path.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    public ViewLocationCacheKey(
        string viewName,
        bool isMainPage)
        : this(
            viewName,
            controllerName: null,
            areaName: null,
            pageName: null,
            isMainPage: isMainPage,
            values: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ViewLocationCacheKey"/>.
    /// </summary>
    /// <param name="viewName">The view name.</param>
    /// <param name="controllerName">The controller name.</param>
    /// <param name="areaName">The area name.</param>
    /// <param name="pageName">The page name.</param>
    /// <param name="isMainPage">Determines if the page being found is the main page for an action.</param>
    /// <param name="values">Values from <see cref="IViewLocationExpander"/> instances.</param>
    public ViewLocationCacheKey(
        string viewName,
        string? controllerName,
        string? areaName,
        string? pageName,
        bool isMainPage,
        IReadOnlyDictionary<string, string?>? values)
    {
        ViewName = viewName;
        ControllerName = controllerName;
        AreaName = areaName;
        PageName = pageName;
        IsMainPage = isMainPage;
        ViewLocationExpanderValues = values;
    }

    /// <summary>
    /// Gets the view name.
    /// </summary>
    public string ViewName { get; }

    /// <summary>
    /// Gets the controller name.
    /// </summary>
    public string? ControllerName { get; }

    /// <summary>
    /// Gets the area name.
    /// </summary>
    public string? AreaName { get; }

    /// <summary>
    /// Gets the page name.
    /// </summary>
    public string? PageName { get; }

    /// <summary>
    /// Determines if the page being found is the main page for an action.
    /// </summary>
    public bool IsMainPage { get; }

    /// <summary>
    /// Gets the values populated by <see cref="IViewLocationExpander"/> instances.
    /// </summary>
    public IReadOnlyDictionary<string, string?>? ViewLocationExpanderValues { get; }

    /// <inheritdoc />
    public bool Equals(ViewLocationCacheKey y)
    {
        if (IsMainPage != y.IsMainPage ||
            !string.Equals(ViewName, y.ViewName, StringComparison.Ordinal) ||
            !string.Equals(ControllerName, y.ControllerName, StringComparison.Ordinal) ||
            !string.Equals(AreaName, y.AreaName, StringComparison.Ordinal) ||
            !string.Equals(PageName, y.PageName, StringComparison.Ordinal))
        {
            return false;
        }

        if (ReferenceEquals(ViewLocationExpanderValues, y.ViewLocationExpanderValues))
        {
            return true;
        }

        if (ViewLocationExpanderValues == null ||
            y.ViewLocationExpanderValues == null ||
            (ViewLocationExpanderValues.Count != y.ViewLocationExpanderValues.Count))
        {
            return false;
        }

        foreach (var item in ViewLocationExpanderValues)
        {
            if (!y.ViewLocationExpanderValues.TryGetValue(item.Key, out var yValue) ||
                !string.Equals(item.Value, yValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ViewLocationCacheKey cacheKey && Equals(cacheKey);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(IsMainPage ? 1 : 0);
        hashCode.Add(ViewName, StringComparer.Ordinal);
        hashCode.Add(ControllerName, StringComparer.Ordinal);
        hashCode.Add(AreaName, StringComparer.Ordinal);
        hashCode.Add(PageName, StringComparer.Ordinal);

        if (ViewLocationExpanderValues != null)
        {
            foreach (var item in ViewLocationExpanderValues)
            {
                hashCode.Add(item.Key, StringComparer.Ordinal);
                hashCode.Add(item.Value, StringComparer.Ordinal);
            }
        }

        return hashCode.ToHashCode();
    }
}

internal static class ViewEnginePath
{
    public static readonly char[] PathSeparators = new[] { '/', '\\' };
    private const string CurrentDirectoryToken = ".";
    private const string ParentDirectoryToken = "..";

    public static string CombinePath(string first, string second)
    {
        Debug.Assert(!string.IsNullOrEmpty(first));

        if (second.StartsWith('/'))
        {
            // "second" is already an app-rooted path. Return it as-is.
            return second;
        }

        string result;

        // Get directory name (including final slash) but do not use Path.GetDirectoryName() to preserve path
        // normalization.
        var index = first.LastIndexOf('/');
        Debug.Assert(index >= 0);

        if (index == first.Length - 1)
        {
            // If the first ends in a trailing slash e.g. "/Home/", assume it's a directory.
            result = first + second;
        }
        else
        {
            result = string.Concat(first.AsSpan(0, index + 1), second);
        }

        return ResolvePath(result);
    }

    public static string ResolvePath(string path)
    {
        Debug.Assert(!string.IsNullOrEmpty(path));
        var pathSegment = new StringSegment(path);
        if (path[0] == PathSeparators[0] || path[0] == PathSeparators[1])
        {
            // Leading slashes (e.g. "/Views/Index.cshtml") always generate an empty first token. Ignore these
            // for purposes of resolution.
            pathSegment = pathSegment.Subsegment(1);
        }

        var tokenizer = new StringTokenizer(pathSegment, PathSeparators);
        var requiresResolution = false;
        foreach (var segment in tokenizer)
        {
            // Determine if we need to do any path resolution.
            // We need to resolve paths with multiple path separators (e.g "//" or "\\") or, directory traversals e.g. ("../" or "./").
            if (segment.Length == 0 ||
                segment.Equals(ParentDirectoryToken, StringComparison.Ordinal) ||
                segment.Equals(CurrentDirectoryToken, StringComparison.Ordinal))
            {
                requiresResolution = true;
                break;
            }
        }

        if (!requiresResolution)
        {
            return path;
        }

        var pathSegments = new List<StringSegment>();
        foreach (var segment in tokenizer)
        {
            if (segment.Length == 0)
            {
                // Ignore multiple directory separators
                continue;
            }

            if (segment.Equals(ParentDirectoryToken, StringComparison.Ordinal))
            {
                if (pathSegments.Count == 0)
                {
                    // Don't resolve the path if we ever escape the file system root. We can't reason about it in a
                    // consistent way.
                    return path;
                }

                pathSegments.RemoveAt(pathSegments.Count - 1);
            }
            else if (segment.Equals(CurrentDirectoryToken, StringComparison.Ordinal))
            {
                // We already have the current directory
                continue;
            }
            else
            {
                pathSegments.Add(segment);
            }
        }

        var builder = new StringBuilder();
        for (var i = 0; i < pathSegments.Count; i++)
        {
            var segment = pathSegments[i];
            builder.Append('/');
            builder.Append(segment.Buffer, segment.Offset, segment.Length);
        }

        return builder.ToString();
    }
}

internal static class NormalizedRouteValue
{
    /// <summary>
    /// Gets the case-normalized route value for the specified route <paramref name="key"/>.
    /// </summary>
    /// <param name="context">The <see cref="ActionContext"/>.</param>
    /// <param name="key">The route key to lookup.</param>
    /// <returns>The value corresponding to the key.</returns>
    /// <remarks>
    /// The casing of a route value in <see cref="ActionContext.RouteData"/> is determined by the client.
    /// This making constructing paths for view locations in a case sensitive file system unreliable. Using the
    /// <see cref="ActionDescriptor.RouteValues"/> to get route values
    /// produces consistently cased results.
    /// </remarks>
    public static string? GetNormalizedRouteValue(ActionContext context, string key)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(key);

        if (!context.RouteData.Values.TryGetValue(key, out var routeValue))
        {
            return null;
        }

        var actionDescriptor = context.ActionDescriptor;
        string? normalizedValue = null;

        if (actionDescriptor.RouteValues.TryGetValue(key, out var value) &&
            !string.IsNullOrEmpty(value))
        {
            normalizedValue = value;
        }

        var stringRouteValue = Convert.ToString(routeValue, CultureInfo.InvariantCulture);
        if (string.Equals(normalizedValue, stringRouteValue, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedValue;
        }

        return stringRouteValue;
    }
}