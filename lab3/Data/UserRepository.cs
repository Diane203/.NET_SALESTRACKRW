using lab3.Models;
using lab3.Services;
using MySqlConnector;

namespace lab3.Data;

public sealed class UserRepository : RepositoryBase
{
    public UserRepository(MySqlConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public async Task<(User? User, string? PasswordHash)> FindByUsernameAsync(string username)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT user_id, username, password_hash, full_name, role, is_active
            FROM users
            WHERE username = @username
            LIMIT 1;
            """;
        AddParameter(command, "@username", username.Trim());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return (null, null);
        }

        var user = new User
        {
            UserId = reader.GetInt32("user_id"),
            Username = reader.GetString("username"),
            FullName = reader.GetString("full_name"),
            Role = Enum.Parse<UserRole>(reader.GetString("role")),
            IsActive = reader.GetBoolean("is_active")
        };

        return (user, reader.GetString("password_hash"));
    }

    public async Task<IReadOnlyList<User>> GetAllAsync()
    {
        var users = new List<User>();
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT user_id, username, full_name, role, is_active FROM users ORDER BY full_name";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User { UserId = reader.GetInt32("user_id"), Username = reader.GetString("username"), FullName = reader.GetString("full_name"), Role = Enum.Parse<UserRole>(reader.GetString("role")), IsActive = reader.GetBoolean("is_active") });
        }
        return users;
    }

    public async Task CreateAsync(string username, string password, string fullName, UserRole role)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO users (username, password_hash, full_name, role) VALUES (@username, @hash, @fullName, @role)";
        AddParameter(command, "@username", username.Trim()); AddParameter(command, "@hash", PasswordHasher.Hash(password)); AddParameter(command, "@fullName", fullName.Trim()); AddParameter(command, "@role", role.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task RegisterAsync(string username, string password, string fullName, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.");
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) throw new ArgumentException("Password must contain at least 6 characters.");

        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO users (username, password_hash, full_name, role)
            VALUES (@username, @hash, @fullName, @role);
            """;
        AddParameter(command, "@username", username.Trim());
        AddParameter(command, "@hash", PasswordHasher.Hash(password));
        AddParameter(command, "@fullName", fullName.Trim());
        AddParameter(command, "@role", role.ToString());

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            throw new InvalidOperationException("That username is already registered. Choose another username.", exception);
        }
    }

    public async Task UpdateAsync(int userId, string fullName, string? password, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required.");
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = string.IsNullOrWhiteSpace(password)
            ? "UPDATE users SET full_name = @name, role = @role WHERE user_id = @id"
            : "UPDATE users SET full_name = @name, password_hash = @hash, role = @role WHERE user_id = @id";
        AddParameter(command, "@id", userId); AddParameter(command, "@name", fullName.Trim()); AddParameter(command, "@role", role.ToString());
        if (!string.IsNullOrWhiteSpace(password)) AddParameter(command, "@hash", PasswordHasher.Hash(password));
        if (await command.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("User was not found.");
    }

    public async Task DeactivateAsync(int userId)
    {
        await using var connection = await ConnectionFactory.OpenConnectionAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "UPDATE users SET is_active = FALSE WHERE user_id = @id"; AddParameter(command, "@id", userId); await command.ExecuteNonQueryAsync();
    }
}
