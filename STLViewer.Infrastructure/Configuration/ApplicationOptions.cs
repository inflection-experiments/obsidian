namespace STLViewer.Infrastructure.Configuration;

/// <summary>
/// Configuration options for application settings.
/// </summary>
public class ApplicationOptions
{
    public const string SectionName = "Application";

    /// <summary>
    /// Gets or sets the application name.
    /// </summary>
    public string Name { get; set; } = "STLViewer";

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the application description.
    /// </summary>
    public string Description { get; set; } = "A modern STL file viewer with 3D rendering capabilities.";

    /// <summary>
    /// Gets or sets the application copyright.
    /// </summary>
    public string Copyright { get; set; } = "Copyright © 2024";

    /// <summary>
    /// Gets or sets the application author.
    /// </summary>
    public string Author { get; set; } = "STLViewer Team";

    /// <summary>
    /// Gets or sets whether telemetry is enabled.
    /// </summary>
    public bool TelemetryEnabled { get; set; } = false;

    /// <summary>
    /// Gets or sets whether auto-updates are enabled.
    /// </summary>
    public bool AutoUpdatesEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the check for updates interval in hours.
    /// </summary>
    public int UpdateCheckIntervalHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the maximum number of recent files to track.
    /// </summary>
    public int MaxRecentFiles { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to show splash screen on startup.
    /// </summary>
    public bool ShowSplashScreen { get; set; } = true;

    /// <summary>
    /// Gets or sets the default culture for localization.
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";
}
