using System.Reflection;
using Microsoft.AspNetCore.WebUtilities;
using Umami.Net.UmamiData.Models;

namespace Umami.Net.UmamiData.Helpers;

/// <summary>
/// Builds query strings with API version-aware parameter mapping.
/// Handles differences between Umami v1 and v2 APIs.
/// </summary>
public static class VersionAwareQueryStringHelper
{
    /// <summary>
    /// Parameter name mappings from v2 (our code) to v1 (legacy API).
    /// </summary>
    private static readonly Dictionary<string, string> V2ToV1ParameterMap = new()
    {
        { "path", "url" },          // v2 uses 'path', v1 uses 'url'
        { "hostname", "host" }      // v2 uses 'hostname', v1 uses 'host'
    };

    /// <summary>
    /// Converts an object to a query string with version-aware parameter mapping.
    /// </summary>
    /// <param name="obj">The request object to convert.</param>
    /// <param name="apiVersion">The target API version.</param>
    /// <returns>A query string compatible with the specified API version.</returns>
    public static string ToQueryString(this object obj, UmamiApiVersion apiVersion)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj),
                "Cannot convert null object to query string. " +
                "Suggestion: Ensure you create and populate a request object before calling ToQueryString().");
        }

        var queryParams = new Dictionary<string, string>();
        var objectType = obj.GetType();

        foreach (var property in objectType.GetProperties())
        {
            var attribute = property.GetCustomAttribute<QueryStringParameterAttribute>();

            // Skip properties without the attribute
            if (attribute == null) continue;

            var propertyValue = property.GetValue(obj);
            var parameterName = string.IsNullOrEmpty(attribute.Name) ? property.Name : attribute.Name;

            // Apply version-specific parameter name mapping
            if (apiVersion == UmamiApiVersion.V1 && V2ToV1ParameterMap.TryGetValue(parameterName, out var mapped))
            {
                parameterName = mapped;
            }

            // Validate required parameters
            if (attribute.IsRequired)
            {
                if (propertyValue == null)
                {
                    throw new ArgumentException(
                        $"Required parameter '{parameterName}' (property '{property.Name}') cannot be null. " +
                        $"Suggestion: Set the {property.Name} property on your {objectType.Name} object before making the API call.",
                        property.Name);
                }

                // For strings, also check for empty/whitespace
                if (propertyValue is string strValue && string.IsNullOrWhiteSpace(strValue))
                {
                    throw new ArgumentException(
                        $"Required parameter '{parameterName}' (property '{property.Name}') cannot be empty or whitespace. " +
                        $"Suggestion: Set {property.Name} to a valid non-empty value.",
                        property.Name);
                }
            }

            // Add non-null values to query string
            if (propertyValue != null)
            {
                var stringValue = ConvertPropertyValue(propertyValue);
                if (!string.IsNullOrEmpty(stringValue))
                {
                    queryParams.Add(parameterName, stringValue);
                }
            }
        }

        return QueryHelpers.AddQueryString(string.Empty, queryParams);
    }

    /// <summary>
    /// Converts a property value to its string representation for query strings.
    /// </summary>
    private static string ConvertPropertyValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            bool b => b.ToString().ToLowerInvariant(),
            Enum e => e.ToString().ToLowerInvariant(),
            DateTime dt => throw new InvalidOperationException(
                $"DateTime value {dt:O} cannot be converted directly. " +
                "Suggestion: Use a calculated property that converts DateTime to Unix milliseconds (see BaseRequest.StartAt/EndAt)."),
            _ => value.ToString() ?? string.Empty
        };
    }
}
