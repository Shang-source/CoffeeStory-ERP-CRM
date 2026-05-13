namespace StoryCoffee.Infrastructure.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "StoryCoffee";
    public string Audience { get; init; } = "StoryCoffee.App";
    public string Secret { get; init; } = "";
    public int ExpiryMinutes { get; init; } = 480;
}
