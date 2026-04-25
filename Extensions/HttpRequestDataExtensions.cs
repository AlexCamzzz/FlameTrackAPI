using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.IdentityModel.Tokens;

namespace FlameTrack.API.Extensions;

public static class HttpRequestDataExtensions
{
    public static string? GetUserId(this HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders)) return null;
        
        var headerValue = authHeaders.FirstOrDefault();
        if (headerValue == null || !headerValue.StartsWith("Bearer ")) return null;

        var token = headerValue.Substring("Bearer ".Length).Trim();
        var secret = Environment.GetEnvironmentVariable("JwtSecret");

        if (string.IsNullOrEmpty(secret)) return null; // No secret, no trust

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false, // Ajustar en prod
                ValidateAudience = false, // Ajustar en prod
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid");
            
            return userIdClaim?.Value;
        }
        catch
        {
            return null; // Firma inválida o expirado
        }
    }
}
