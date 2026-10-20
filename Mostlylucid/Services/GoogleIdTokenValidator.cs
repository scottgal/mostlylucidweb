using Google.Apis.Auth;
using Mostlylucid.Shared.Config;

namespace Mostlylucid.Services;

public interface IGoogleIdTokenValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string token);
}

public class GoogleIdTokenValidator(AuthSettings auth) : IGoogleIdTokenValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(auth.GoogleClientId))
            throw new InvalidOperationException("Google sign-in is not configured.");

        // Google validates the signature, issuer, lifetime and our client ID as the audience.
        return GoogleJsonWebSignature.ValidateAsync(token, new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { auth.GoogleClientId }
        });
    }
}
