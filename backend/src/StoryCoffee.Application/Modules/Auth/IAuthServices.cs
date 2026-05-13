namespace StoryCoffee.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    (string Token, int ExpiresIn) Create(StoryCoffee.Domain.User user);
    System.Security.Claims.ClaimsPrincipal? Validate(string token);
}
