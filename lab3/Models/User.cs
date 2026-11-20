namespace lab3.Models;

public enum UserRole
{
    Administrator,
    Cashier
}

public sealed class User
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
}
