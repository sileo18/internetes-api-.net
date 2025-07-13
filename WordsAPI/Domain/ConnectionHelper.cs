using Npgsql;

namespace WordsAPI.Domain;

public class ConnectionHelper
{
    // Em ConnectionHelper.cs
public static string GetConnectionString(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"[DEBUG] ConnectionString 'DefaultConnection' from config: {(string.IsNullOrEmpty(connectionString) ? "NULL/EMPTY" : connectionString)}");

    if (string.IsNullOrEmpty(connectionString))
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        Console.WriteLine($"[DEBUG] DATABASE_URL from env inside GetConnectionString: {(string.IsNullOrEmpty(databaseUrl) ? "NULL/EMPTY" : databaseUrl)}");
        if (!string.IsNullOrEmpty(databaseUrl))
        {
            connectionString = BuildConnectionString(databaseUrl);
        }
    }

    Console.WriteLine($"[DEBUG] Final connection string before return: {(string.IsNullOrEmpty(connectionString) ? "NULL/EMPTY" : connectionString)}");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found");
    }

    return connectionString;
}

private static string BuildConnectionString(string databaseUrl)
{
    try {
        Console.WriteLine($"[DEBUG] Attempting to build connection string from URL: {databaseUrl}");
        var databaseUri = new Uri(databaseUrl);
        Console.WriteLine($"[DEBUG] Parsed URI - Host: {databaseUri.Host}, Port: {databaseUri.Port}, UserInfo: {databaseUri.UserInfo}, Path: {databaseUri.LocalPath}");

        var userInfo = databaseUri.UserInfo.Split(':');
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = databaseUri.LocalPath.TrimStart('/'),
        };
        var finalBuiltString = builder.ToString();
        Console.WriteLine($"[DEBUG] Connection String built by NpgsqlConnectionStringBuilder: {finalBuiltString}");
        return finalBuiltString;
    } catch (Exception ex) {
        Console.WriteLine($"[ERROR] Failed to build connection string from URL '{databaseUrl}': {ex.Message}");
        throw;
    }
}
}