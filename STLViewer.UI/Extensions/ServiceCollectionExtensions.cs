using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using STLViewer.UI.ViewModels;
using STLViewer.UI.Views;

namespace STLViewer.UI.Extensions;

/// <summary>
/// Extension methods for configuring UI layer dependency injection services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds UI layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUIServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register ViewModels
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<Viewport3DViewModel>();

        // Register Views
        services.AddTransient<MainWindow>();

        return services;
    }
}
