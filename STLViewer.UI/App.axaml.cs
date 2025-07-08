using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using STLViewer.UI.ViewModels;
using STLViewer.UI.Views;
using System;

namespace STLViewer.UI;

public partial class App : Avalonia.Application
{
    private ILogger<App>? _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Get logger from DI container if available
        try
        {
            _logger = ServiceLocator.GetService<ILogger<App>>();
            _logger?.LogInformation("Application initializing...");
        }
        catch (Exception ex)
        {
            // Fallback logging if DI is not yet available
            Console.WriteLine($"Failed to get logger during initialization: {ex.Message}");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            _logger?.LogInformation("Framework initialization completed");

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Resolve MainWindow and its ViewModel from DI container
                var mainWindow = ServiceLocator.GetRequiredService<MainWindow>();
                var mainWindowViewModel = ServiceLocator.GetRequiredService<MainWindowViewModel>();

                mainWindow.DataContext = mainWindowViewModel;
                desktop.MainWindow = mainWindow;

                _logger?.LogInformation("Main window created and configured");
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
            {
                // For mobile platforms
                var mainWindowViewModel = ServiceLocator.GetRequiredService<MainWindowViewModel>();
                singleViewPlatform.MainView = new MainWindow
                {
                    DataContext = mainWindowViewModel
                };

                _logger?.LogInformation("Single view application configured");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to complete framework initialization");

            // Fallback to create window without DI if something goes wrong
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(
                        new STLViewer.Infrastructure.Parsers.STLParserService())
                };
            }
        }

        base.OnFrameworkInitializationCompleted();

        _logger?.LogInformation("Application startup completed");
    }

    /// <summary>
    /// Handles unhandled exceptions in the application.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The exception event arguments.</param>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");

        // In a production app, you might want to show an error dialog here
        // and possibly save the user's work before shutting down
    }

    /// <summary>
    /// Called when the application is shutting down.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The shutdown event arguments.</param>
    private void OnApplicationShutdown(object sender, EventArgs e)
    {
        _logger?.LogInformation("Application shutting down");

        // Perform cleanup operations here
        // Save user settings, dispose resources, etc.
    }
}
