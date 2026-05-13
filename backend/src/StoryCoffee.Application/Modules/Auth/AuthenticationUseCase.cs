namespace StoryCoffee.Application.Auth;

public sealed class AuthenticationUseCase(
    IUserRepository users,
    IClock clock,
    IPasswordHasher passwordHasher,
    IJwtTokenService tokenService)
{
    public async Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindActiveByEmailWithCustomer(request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ApiException(401, "UNAUTHORIZED", "Invalid email or password.");
        }

        if (user.Role == UserRole.Customer && user.Customer?.AccountStatus is AccountStatus.Suspended or AccountStatus.Archived)
        {
            throw new ApiException(403, "customer_account_inactive", "Customer account is not active.");
        }

        user.LastLoginAt = clock.UtcNow;
        user.UpdatedAt = clock.UtcNow;
        await users.SaveChanges(cancellationToken);

        var token = tokenService.Create(user);
        var profile = new UserProfileDto(user.Id, user.Email, user.Role, user.CustomerId, user.DisplayName);
        return new LoginResponse(token.Token, token.ExpiresIn, user.Role, profile);
    }

    public async Task ChangeCustomerPassword(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new ApiException(400, "INVALID_PASSWORD", "New password must be at least 8 characters.");
        }

        if (request.NewPassword != request.ConfirmNewPassword)
        {
            throw new ApiException(400, "PASSWORD_MISMATCH", "New password confirmation does not match.");
        }

        var user = await users.FindActiveById(userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ApiException(400, "INVALID_CURRENT_PASSWORD", "Current password is incorrect.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = clock.UtcNow;
        users.AddAudit("ChangedPassword", "User", user.Id, $"Changed password for {user.Email}", user.Id, user.Role.ToString());
        await users.SaveChanges(cancellationToken);
    }
}
