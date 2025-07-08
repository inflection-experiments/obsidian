using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using STLViewer.Core.Interfaces;
using STLViewer.Infrastructure.Examples;
using STLViewer.Infrastructure.Graphics;
using STLViewer.Infrastructure.Parsers;
using STLViewer.Infrastructure.Plugins;
using Serilog;
using AutoMapper;
using MediatR;
using FluentValidation;
using System.Reflection;
using STLViewer.Infrastructure.Services;
using STLViewer.Infrastructure.Repositories;

namespace STLViewer.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console()
            .WriteTo.File("logs/stlviewer-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add core services
        services.AddScoped<ISTLParser, STLParserService>();
        services.AddScoped<IFileManagementService, FileManagementService>();
        services.AddScoped<IMeasurementService, MeasurementService>();
        services.AddSingleton<IModelRepository, Infrastructure.Repositories.InMemoryModelRepository>();
        services.AddSingleton<IMeasurementSessionRepository, Infrastructure.Repositories.InMemoryMeasurementSessionRepository>();

        // Graphics and rendering
        services.AddTransient<ICamera, Camera>();
        services.AddTransient<ICameraAnimationService, CameraAnimationService>();
        services.AddSingleton<ILightingService, LightingService>();
        // Note: RendererFactory is static and doesn't need DI registration

        // Add pre-loaded model generators
        services.AddScoped<FighterPlaneModelGenerator>();
        services.AddScoped<IPreloadedModelGenerator, FighterPlaneModelGenerator>();

        // Add scene plugin services
        services.AddSingleton<IScenePluginManager, Infrastructure.Plugins.ScenePluginManager>();

        // Add HTTP client with Polly for resilience
        services.AddHttpClient("STLViewer", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    /// <summary>
    /// Adds application layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>
    /// Adds configuration services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind strongly-typed configuration sections
        services.Configure<Configuration.RenderingOptions>(
            configuration.GetSection(Configuration.RenderingOptions.SectionName));

        services.Configure<Configuration.ApplicationOptions>(
            configuration.GetSection(Configuration.ApplicationOptions.SectionName));

        services.Configure<Configuration.LoggingOptions>(
            configuration.GetSection(Configuration.LoggingOptions.SectionName));

        return services;
    }

    /// <summary>
    /// Initializes the plugin system with default plugins.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static STLViewer.Domain.Common.Result InitializePlugins(this IServiceProvider serviceProvider)
    {
        try
        {
            var pluginManager = serviceProvider.GetRequiredService<IScenePluginManager>();
            var result = ((ScenePluginManager)pluginManager).RegisterDefaultPlugins();

            if (result.IsFailure)
            {
                return result;
            }

            // Enable the flight path plugin by default
            var enableResult = pluginManager.EnablePlugin("flight-path-visualizer");
            return enableResult;
        }
        catch (Exception ex)
        {
            return STLViewer.Domain.Common.Result.Fail($"Failed to initialize plugins: {ex.Message}");
        }
    }
}

/// <summary>
/// Configuration options for rendering settings.
/// </summary>
public class RenderingOptions
{
    /// <summary>
    /// Gets or sets the default renderer type.
    /// </summary>
    public string DefaultRenderer { get; set; } = "OpenGL";

    /// <summary>
    /// Gets or sets the anti-aliasing level.
    /// </summary>
    public int AntiAliasing { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum frame rate.
    /// </summary>
    public int MaxFrameRate { get; set; } = 60;
}

/// <summary>
/// Configuration options for application settings.
/// </summary>
public class ApplicationOptions
{
    /// <summary>
    /// Gets or sets the application title.
    /// </summary>
    public string Title { get; set; } = "STL Viewer";

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets whether to show debug information.
    /// </summary>
    public bool ShowDebugInfo { get; set; } = false;

    /// <summary>
    /// Gets or sets the default window width.
    /// </summary>
    public int DefaultWindowWidth { get; set; } = 1200;

    /// <summary>
    /// Gets or sets the default window height.
    /// </summary>
    public int DefaultWindowHeight { get; set; } = 800;
}

/// <summary>
/// Configuration options for logging settings.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    public string MinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets whether to write logs to file.
    /// </summary>
    public bool WriteToFile { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to write logs to console.
    /// </summary>
    public bool WriteToConsole { get; set; } = true;

    /// <summary>
    /// Gets or sets the log file path template.
    /// </summary>
    public string FilePathTemplate { get; set; } = "logs/stlviewer-.txt";
}
