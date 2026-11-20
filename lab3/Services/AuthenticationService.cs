using lab3.Data;
using lab3.Models;

namespace lab3.Services;

public sealed class AuthenticationService
{
    private readonly UserRepository _userRepository;

    public AuthenticationService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(bool Success, User? User, string Message)> LoginAsync(string username, string password, UserRole selectedRole)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, null, "Username and password are required.");
        }

        var result = await _userRepository.FindByUsernameAsync(username);
        if (result.User is null || result.PasswordHash is null ||
            !PasswordHasher.Verify(password, result.PasswordHash))
        {
            return (false, null, "Invalid username or password.");
        }

        if (!result.User.IsActive)
        {
            return (false, null, "This user account is inactive.");
        }

        if (result.User.Role != selectedRole)
        {
            var actualRole = result.User.Role == UserRole.Administrator ? "Administrator" : "Salesperson / Cashier";
            return (false, null, $"This account is registered as {actualRole}. Choose the matching role.");
        }

        return (true, result.User, "Login successful.");
    }
}
