using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StoryCoffee.Domain;
using StoryCoffee.Infrastructure.Options;

namespace StoryCoffee.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions) : IJwtTokenService
{
    private readonly JwtOptions options = jwtOptions.Value;

    public (string Token, int ExpiresIn) Create(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.ExpiryMinutes);
        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };
        var payload = new Dictionary<string, object?>
        {
            ["iss"] = options.Issuer,
            ["aud"] = options.Audience,
            ["sub"] = user.Id.ToString(),
            ["email"] = user.Email,
            ["role"] = user.Role.ToString(),
            ["name"] = user.DisplayName,
            ["customerId"] = user.CustomerId?.ToString(),
            ["exp"] = expiresAt.ToUnixTimeSeconds()
        };

        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload));
        var signature = Sign($"{encodedHeader}.{encodedPayload}");
        return ($"{encodedHeader}.{encodedPayload}.{signature}", options.ExpiryMinutes * 60);
    }

    public ClaimsPrincipal? Validate(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var unsignedToken = $"{parts[0]}.{parts[1]}";
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Sign(unsignedToken)), Encoding.UTF8.GetBytes(parts[2])))
        {
            return null;
        }

        using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        var root = payload.RootElement;
        if (!root.TryGetProperty("exp", out var expElement) || DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expElement.GetInt64())
        {
            return null;
        }

        if (root.GetProperty("iss").GetString() != options.Issuer || root.GetProperty("aud").GetString() != options.Audience)
        {
            return null;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, root.GetProperty("sub").GetString() ?? ""),
            new(ClaimTypes.Email, root.GetProperty("email").GetString() ?? ""),
            new(ClaimTypes.Role, root.GetProperty("role").GetString() ?? ""),
            new(ClaimTypes.Name, root.GetProperty("name").GetString() ?? "")
        };

        if (root.TryGetProperty("customerId", out var customerIdElement) && customerIdElement.ValueKind == JsonValueKind.String)
        {
            claims.Add(new Claim("customerId", customerIdElement.GetString() ?? ""));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "StoryCoffeeJwt"));
    }

    private string Sign(string value)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Secret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
