using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;

namespace FlameTrack.API.Extensions;

public static class HttpRequestDataExtensions
{
    public static string? GetUserId(this HttpRequestData req)
    {
        if (req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            var headerValue = authHeaders.FirstOrDefault();
            if (headerValue != null && headerValue.StartsWith("Bearer "))
            {
                var token = headerValue.Substring("Bearer ".Length).Trim();
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);
                    
                    var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == ClaimTypes.NameIdentifier);
                    return userIdClaim?.Value;
                }
                catch
                {
                    // Invalid token format
                    return null;
                }
            }
        }
        return null;
    }
}
