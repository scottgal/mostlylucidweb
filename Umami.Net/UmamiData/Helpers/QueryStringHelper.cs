using System.Reflection;
using Microsoft.AspNetCore.WebUtilities;

namespace Umami.Net.UmamiData.Helpers;

/// <summary>
/// Helper class for converting objects to query strings using attributes.
/// </summary>
public static class QueryStringHelper
{
    /// <summary>
    /// Converts an object to a URL query string based on <see cref="QueryStringParameterAttribute"/> decorations.
    /// </summary>
    /// <param name="obj">The object to convert to a query string.</param>
    /// <returns>A query string with all non-null properties included.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when obj is null.
    /// Suggestion: Ensure you pass a valid request object.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a required parameter is null.
    /// Suggestion: Set all required properties before calling this method.
    /// </exception>
    public static string ToQueryString(this object obj) =>
        VersionAwareQueryStringHelper.ToQueryString(obj, Models.UmamiApiVersion.V2);
}
