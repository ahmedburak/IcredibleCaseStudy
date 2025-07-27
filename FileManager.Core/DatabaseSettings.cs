namespace FileManager.Database;

/// <summary>
/// Database configuration settings
/// </summary>
public class DatabaseSettings
{
    /// <summary>
    /// Database host
    /// </summary>
    public required string Host { get; set; }

    /// <summary>
    /// Database name
    /// </summary>
    public required string Database { get; set; }

    /// <summary>
    /// Database username
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// Database password
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Database port
    /// </summary>
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Get connection string
    /// </summary>
    /// <returns>PostgreSQL connection string</returns>
    public string GetConnectionString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};";
    }
}