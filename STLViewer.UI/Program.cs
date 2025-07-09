using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STLViewer.Infrastructure.Extensions;
using STLViewer.Application.Extensions;
using STLViewer.UI.Extensions;
using STLViewer.UI.ViewModels;
using STLViewer.UI.Views;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace STLViewer.UI;

/// <summary>
/// The main program entry point.
/// </summary>
public class Program
{
    private static IHost? _host;

    /// <summary>
    /// Initialization code. Don't use any Avalonia, third-party APIs or any
    /// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    /// yet and stuff might break.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    [STAThread]
    public static async Task Main(string[] args)
    {
        try
        {
            // Create and configure the host
            _host = CreateHostBuilder(args).Build();

            // Start the host
            await _host.StartAsync();

            // Build and run the Avalonia application
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
    }

    /// <summary>
    /// Creates the host builder with dependency injection and configuration.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>The configured host builder.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Add configuration sources
                config.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                // Add all application services
                services.AddInfrastructureServices(context.Configuration);
                services.AddApplicationServices();
                services.AddUIServices(context.Configuration);
                services.AddConfiguration(context.Configuration);
            })
            .UseSerilog((context, services, configuration) =>
            {
                configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("logs/stlviewer-.txt", rollingInterval: RollingInterval.Day);
            });

    /// <summary>
    /// Avalonia configuration, don't remove; also used by visual designer.
    /// </summary>
    /// <returns>The configured Avalonia app builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();

    /// <summary>
    /// Gets a required service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    public static T GetRequiredService<T>() where T : notnull
    {
        if (_host == null)
            throw new InvalidOperationException("Host has not been initialized");

        return _host.Services.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not found.</returns>
    public static T? GetService<T>()
    {
        if (_host == null)
            return default;

        return _host.Services.GetService<T>();
    }

    /// <summary>
    /// Gets the service scope factory for creating scoped services.
    /// </summary>
    /// <returns>The service scope factory.</returns>
    public static IServiceScopeFactory GetServiceScopeFactory()
    {
        return GetRequiredService<IServiceScopeFactory>();
    }
}

/// <summary>
/// Service locator for accessing services in XAML and other scenarios where DI is not available.
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// Gets a required service from the application's dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    public static T GetRequiredService<T>() where T : notnull
    {
        return Program.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service from the application's dependency injection container.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not found.</returns>
    public static T? GetService<T>()
    {
        return Program.GetService<T>();
    }

    /// <summary>
    /// Creates a new scope for scoped services.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public static IServiceScope CreateScope()
    {
        return Program.GetServiceScopeFactory().CreateScope();
    }
}
