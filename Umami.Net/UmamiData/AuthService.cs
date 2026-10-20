using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public class AuthService(HttpClient httpClient, UmamiDataSettings umamiSettings, ILogger<AuthService> logger)
{
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    public HttpClient HttpClient => httpClient;

    public Task<bool> Login(bool skipVerify = false) => LoginAsync(skipVerify, CancellationToken.None);

    public async Task<bool> LoginAsync(bool skipVerify = false, CancellationToken cancellationToken = default)
    {
        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(umamiSettings.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", umamiSettings.ApiKey);
                return true; // API keys cannot call /auth/verify on self-hosted 3.4.
            }
            if (!skipVerify && httpClient.DefaultRequestHeaders.Authorization != null && await VerifyTokenAsync(false, cancellationToken))
                return true;
            using var response = await httpClient.PostAsJsonAsync("api/auth/login",
                new { username = umamiSettings.Username, password = umamiSettings.Password }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
                if (auth?.RequiresTwoFactor != true && !string.IsNullOrWhiteSpace(auth?.Token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    logger.LogInformation("Login successful");
                    return true;
                }
            }
            httpClient.DefaultRequestHeaders.Authorization = null;
            logger.LogWarning("Login failed");
            return false;
        }
        catch (JsonException)
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
            logger.LogWarning("Login failed: invalid authentication response");
            return false;
        }
        finally { _loginLock.Release(); }
    }

    public Task<bool> VerifyToken(bool isTest = false) => VerifyTokenAsync(isTest, CancellationToken.None);

    private async Task<bool> VerifyTokenAsync(bool isTest, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(umamiSettings.ApiKey))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, (umamiSettings.UseCloudApi ? "" : "api/") + "me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", umamiSettings.ApiKey);
            using var verified = await httpClient.SendAsync(request, cancellationToken);
            return verified.IsSuccessStatusCode;
        }
        using var response = await httpClient.PostAsync("api/auth/verify" + (isTest ? "?test" : ""), null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    internal async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> createRequest, CancellationToken cancellationToken)
    {
        if (!await LoginAsync(cancellationToken: cancellationToken))
            return new HttpResponseMessage(HttpStatusCode.Unauthorized) { ReasonPhrase = "Failed to login" };
        for (var attempt = 0; ; attempt++)
        {
            using var request = createRequest();
            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized || attempt == 1 || !string.IsNullOrWhiteSpace(umamiSettings.ApiKey))
                return response;
            response.Dispose();
            if (!await LoginAsync(true, cancellationToken))
                return new HttpResponseMessage(HttpStatusCode.Unauthorized) { ReasonPhrase = "Failed to login" };
        }
    }
}
