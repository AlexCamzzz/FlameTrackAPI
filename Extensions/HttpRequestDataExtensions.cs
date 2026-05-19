using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace FlameTrack.API.Extensions;

public static class HttpRequestDataExtensions
{
    public static string? GetUserId(this HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders) || authHeaders == null) return null;
        
        var headerValue = authHeaders.FirstOrDefault();
        if (headerValue == null || !headerValue.StartsWith("Bearer ")) return null;

        return ValidateToken(headerValue.Substring("Bearer ".Length).Trim());
    }

    public static string? GetUserId(this HttpRequest req)
    {
        string? authHeader = req.Headers["Authorization"];
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ")) return null;

        return ValidateToken(authHeader.Substring("Bearer ".Length).Trim());
    }

    private static string? ValidateToken(string token)
    {
        var secret = Environment.GetEnvironmentVariable("JwtSecret");
        if (string.IsNullOrEmpty(secret)) return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
            
            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }
}
