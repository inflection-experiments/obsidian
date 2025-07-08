namespace STLViewer.Infrastructure.Configuration;

/// <summary>
/// Configuration options for logging settings.
/// </summary>
public class LoggingOptions
{
    public const string SectionName = "Logging";

    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether console logging is enabled.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether file logging is enabled.
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the log file path template.
    /// </summary>
    public string LogFilePathTemplate { get; set; } = "logs/stlviewer-.txt";

    /// <summary>
    /// Gets or sets the rolling interval for log files.
    /// </summary>
    public string RollingInterval { get; set; } = "Day";

    /// <summary>
    /// Gets or sets the maximum number of log files to retain.
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to include scopes in log output.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include timestamps in log output.
    /// </summary>
    public bool IncludeTimestamps { get; set; } = true;

    /// <summary>
    /// Gets or sets the log output template.
    /// </summary>
    public string OutputTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
}
