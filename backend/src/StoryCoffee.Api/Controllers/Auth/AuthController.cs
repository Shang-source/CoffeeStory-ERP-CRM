using Microsoft.AspNetCore.Mvc;
using StoryCoffee.Contracts;
using StoryCoffee.Domain;

namespace StoryCoffee.Api.Controllers;

public sealed class AuthController(AuthenticationUseCase auth) : StoryCoffeeController
{
    [HttpPost("api/auth/login")]
    public async Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return await auth.Login(request, cancellationToken);
    }

    [HttpPost("api/customer/password")]
    public async Task<IActionResult> ChangeCustomerPassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        RequireRole(UserRole.Customer);
        await auth.ChangeCustomerPassword(CurrentUserId(), request, cancellationToken);
        return NoContent();
    }
}
