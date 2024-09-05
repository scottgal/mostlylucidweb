using System.Reflection;
using Microsoft.AspNetCore.WebUtilities;

namespace Umami.Net.UmamiData.Helpers;

public static class QueryStringHelper
{
    public static string ToQueryString(this object obj)
    {
        var queryParams = new Dictionary<string, string>();

        foreach (var property in obj.GetType().GetProperties())
        {
            var attribute = property.GetCustomAttribute<QueryStringParameterAttribute>();
            if(attribute is { IsRequired: true })
            {
                var thisValue = property.GetValue(obj);
                if(thisValue == null)
                {
                    throw new ArgumentException($"Property {property.Name} is required and cannot be null");
                }
            }
            if(attribute==null) continue;
            var propertyName = string.IsNullOrEmpty( attribute.Name) ? property.Name : attribute.Name; 
            var propertyValue = property.GetValue(obj);
            if (propertyValue != null)
            {
                // Add the parameter to the dictionary, converting it to a string
                queryParams.Add(propertyName, propertyValue?.ToString() ?? string.Empty);
            }
        }
        
        return QueryHelpers.AddQueryString(string.Empty, queryParams);
    }
}