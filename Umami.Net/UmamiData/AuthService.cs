using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Umami.Net.Config;
using Umami.Net.UmamiData.Models.ResponseObjects;

namespace Umami.Net.UmamiData;

public class AuthService(HttpClient httpClient, UmamiDataSettings umamiSettings, ILogger<AuthService> logger)
{
    private string _token = string.Empty;
    public HttpClient HttpClient => httpClient;

    /// <summary>
    /// Logs in to the Umami API
    /// </summary>
    /// <param name="skipVerify">Mostly used for testing, this will skip the VerifyToken stage</param>
    /// <returns>True or False depending on whether the login succeeded</returns>
    public async Task<bool> Login(bool skipVerify = false)
    {
        if(!skipVerify && await VerifyToken()) return true;
        var loginData = new
        {
            username = umamiSettings.Username,
            password = umamiSettings.Password
        };

        var content = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync("/api/auth/login", content);

        if (response.IsSuccessStatusCode)
        {
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null)
            {
                logger.LogError("Login failed");
                return false;
            }

            _token = authResponse.Token;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            logger.LogInformation("Login successful");
            return true;
        }

        logger.LogError("Login failed");
        return false;
    }
    
    public async Task<bool> VerifyToken(bool isTest=false)
    {
        var verify = await httpClient.GetAsync("/api/auth/verify" + (isTest ? "?test" : ""));
        if(verify.IsSuccessStatusCode == false)
        {
            logger.LogError("Verify failed");
            return false;
        }
        logger.LogInformation("Verify successful");
        return true;
        
    }
}