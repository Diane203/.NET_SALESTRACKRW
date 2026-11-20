using lab3.Models;
using lab3.Services;

namespace lab3.Controllers;

public sealed class LoginController
{
    private readonly AuthenticationService _authenticationService;

    public LoginController(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    public Task<(bool Success, User? User, string Message)> LoginAsync(string username, string password, UserRole selectedRole)
    {
        return _authenticationService.LoginAsync(username, password, selectedRole);
    }
}
