using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Vamoriq.Services.Utilities;

public static class JwtUtility
{
    public static bool IsTokenExpired(string token)
    {
        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwtToken.ValidTo <= DateTime.UtcNow;
        }
        catch
        {

            return true;
        }
    }

    public static bool IsTokenExpiringWithin(string token, TimeSpan timeWindow)
    {
        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwtToken.ValidTo <= DateTime.UtcNow.Add(timeWindow);
        }
        catch
        {
            return true;
        }
    }

    public static DateTime? GetTokenExpiration(string token)
    {
        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    public static long? GetExpiryUnixSeconds(string token)
    {
        var dt = GetTokenExpiration(token);
        if (!dt.HasValue) return null;
        var unix = new DateTimeOffset(dt.Value).ToUnixTimeSeconds();
        return unix;
    }

    public static string? GetClaim(string token, string claimType)
    {
        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public static string? GetUserId(string token)
    {
        return GetClaim(token, ClaimTypes.NameIdentifier) ?? GetClaim(token, "sub");
    }

    public static string? GetUserEmail(string token)
    {
        return GetClaim(token, ClaimTypes.Email) ?? GetClaim(token, "email");
    }

    public static string? GetUserName(string token)
    {
        return GetClaim(token, ClaimTypes.Name) ?? GetClaim(token, "preferred_username");
    }

    public static string? GetFirstName(string token)
    {
        return GetClaim(token, ClaimTypes.GivenName) ?? GetClaim(token, "given_name");
    }

    public static string? GetLastName(string token)
    {
        return GetClaim(token, ClaimTypes.Surname) ?? GetClaim(token, "family_name");
    }

    public static bool? GetEmailVerified(string token)
    {
        var raw = GetClaim(token, "email_verified");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (bool.TryParse(raw, out var b))
        {
            return b;
        }

        if (raw == "1") return true;
        if (raw == "0") return false;

        return null;
    }
}
