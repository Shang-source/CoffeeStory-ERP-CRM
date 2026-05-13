using StoryCoffee.Domain;

namespace StoryCoffee.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    UserRole Role,
    UserProfileDto UserProfile);

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    UserRole Role,
    Guid? CustomerId,
    string Name);
