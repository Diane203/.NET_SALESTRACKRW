using System.Text.Json;

namespace lab3.Data;

public sealed class DatabaseSettings
{
    public string ConnectionString { get; init; } = string.Empty;

    public static DatabaseSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Database configuration file was not found.", path);
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("SalesTrack")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The SalesTrack connection string is empty.");
        }

        return new DatabaseSettings { ConnectionString = connectionString };
    }
}
