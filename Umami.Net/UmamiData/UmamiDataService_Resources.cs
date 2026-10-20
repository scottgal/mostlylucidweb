using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Umami.Net.UmamiData.Models;

namespace Umami.Net.UmamiData;

public partial class UmamiDataService
{
    /// <summary>Access additional documented website resources without hard-coding a server host or losing structured responses.</summary>
    public Task<UmamiResult<T>> SendWebsiteRequestAsync<T>(HttpMethod method, string resource,
        IReadOnlyDictionary<string, string?>? query = null, object? body = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);
        if (string.IsNullOrWhiteSpace(resource) || resource.Split('/').Any(segment => segment is "" or "." or "..")
            || resource.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '/')))
            throw new ArgumentException("Use a relative website resource path, without a query string or dot segments", nameof(resource));
        var path = WebsitePath(resource);
        if (query != null) path = QueryHelpers.AddQueryString(path, query);
        return RequestAsync<T>(method, path, body, cancellationToken);
    }
}
