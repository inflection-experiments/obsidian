using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STLViewer.Core.Interfaces;
using STLViewer.Infrastructure.Extensions;
using STLViewer.Infrastructure.Graphics;
using STLViewer.Infrastructure.Parsers;
using Xunit;
using Polly;
using System.IO;
using System.Text.Json;

namespace STLViewer.Infrastructure.Tests.Extensions;

/// <summary>
/// Tests for dependency injection service registration extensions.
/// </summary>
public class ServiceCollectionExtensionsTests : IDisposable
{
    private readonly ServiceCollection _services;
    private readonly IConfigurationBuilder _configBuilder;

    public ServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection();
        _configBuilder = new ConfigurationBuilder();

        // Create a minimal test configuration
        var testConfig = new
        {
            Serilog = new
            {
                MinimumLevel = "Information",
                WriteTo = new[]
                {
                    new { Name = "Console" }
                }
            },
            ConnectionStrings = new
            {
                DefaultConnection = "Server=localhost;Database=STLViewer;Trusted_Connection=true;"
            },
            Application = new
            {
                Name = "STLViewer",
                Version = "1.0.0"
            },
            Rendering = new
            {
                DefaultRenderer = "OpenGL",
                MaxTextureSize = 4096,
                EnableVSync = true,
                AntiAliasing = 4
            }
        };

        var json = JsonSerializer.Serialize(testConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText("test-appsettings.json", json);

        _configBuilder.AddJsonFile("test-appsettings.json", optional: false, reloadOnChange: false);
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterCoreServices()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ISTLParser>().Should().NotBeNull();
        serviceProvider.GetService<ISTLParser>().Should().BeOfType<STLParserService>();

        serviceProvider.GetService<ICamera>().Should().NotBeNull();
        serviceProvider.GetService<ICamera>().Should().BeOfType<Camera>();
    }

    [Fact]
    public void AddInfrastructureServices_ShouldRegisterHttpClientWithPolly()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.Should().NotBeNull();

        var httpClient = httpClientFactory!.CreateClient("STLViewer");
        httpClient.Should().NotBeNull();
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddInfrastructureServices_ShouldConfigureLogging()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();

        var logger = serviceProvider.GetService<ILogger<ServiceCollectionExtensionsTests>>();
        logger.Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterMediatR()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        _services.AddApplicationServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetServices<MediatR.ISender>().Should().NotBeEmpty();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterAutoMapper()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        _services.AddApplicationServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<AutoMapper.IMapper>().Should().NotBeNull();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterFluentValidation()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        _services.AddApplicationServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        serviceProvider.GetServices<FluentValidation.IValidator>().Should().NotBeNull();
    }

    [Fact]
    public void AddConfiguration_ShouldBindStronglyTypedOptions()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddConfiguration(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var renderingOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<STLViewer.Infrastructure.Configuration.RenderingOptions>>();
        renderingOptions.Should().NotBeNull();

        var applicationOptions = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<STLViewer.Infrastructure.Configuration.ApplicationOptions>>();
        applicationOptions.Should().NotBeNull();
    }

    [Fact]
    public void ServiceRegistration_ShouldHaveCorrectLifetimes()
    {
        // Arrange
        var configuration = _configBuilder.Build();

        // Act
        _services.AddInfrastructureServices(configuration);
        var descriptors = _services.ToList();

        // Assert
        var stlParserDescriptor = descriptors.FirstOrDefault(d => d.ServiceType == typeof(ISTLParser));
        stlParserDescriptor.Should().NotBeNull();
        stlParserDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var cameraDescriptor = descriptors.FirstOrDefault(d => d.ServiceType == typeof(ICamera));
        cameraDescriptor.Should().NotBeNull();
        cameraDescriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void PolicyConfiguration_ShouldCreateRetryPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.CreateRetryPolicy();

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeAssignableTo<IAsyncPolicy<HttpResponseMessage>>();
    }

    [Fact]
    public void PolicyConfiguration_ShouldCreateCircuitBreakerPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.CreateCircuitBreakerPolicy();

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeAssignableTo<IAsyncPolicy<HttpResponseMessage>>();
    }

    [Fact]
    public void PolicyConfiguration_ShouldCreateTimeoutPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.CreateTimeoutPolicy();

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeAssignableTo<IAsyncPolicy<HttpResponseMessage>>();
    }

    [Fact]
    public void PolicyConfiguration_ShouldCreateBulkheadPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.CreateBulkheadPolicy();

        // Assert
        policy.Should().NotBeNull();
        policy.Should().BeAssignableTo<IAsyncPolicy<HttpResponseMessage>>();
    }

    [Fact]
    public void PolicyConfiguration_ShouldCreateCombinedPolicy()
    {
        // Arrange & Act
        var policy = PollyPolicies.CreateCombinedPolicy();

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public void HttpClientConfiguration_ShouldSetCorrectTimeout()
    {
        // Arrange
        var configuration = _configBuilder.Build();
        _services.AddInfrastructureServices(configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = httpClientFactory.CreateClient("STLViewer");

        // Assert
        client.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void ServiceProvider_ShouldResolveAllRegisteredServices()
    {
        // Arrange
        var configuration = _configBuilder.Build();
        _services.AddInfrastructureServices(configuration);
        _services.AddApplicationServices(configuration);
        _services.AddConfiguration(configuration);

        // Act
        var serviceProvider = _services.BuildServiceProvider();

        // Assert - Core services
        serviceProvider.GetRequiredService<ISTLParser>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ICamera>().Should().NotBeNull();
        serviceProvider.GetRequiredService<IHttpClientFactory>().Should().NotBeNull();
        serviceProvider.GetRequiredService<ILoggerFactory>().Should().NotBeNull();

        // Assert - Application services
        serviceProvider.GetRequiredService<MediatR.ISender>().Should().NotBeNull();
        serviceProvider.GetRequiredService<AutoMapper.IMapper>().Should().NotBeNull();

        // Assert - Configuration
        serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<STLViewer.Infrastructure.Configuration.RenderingOptions>>().Should().NotBeNull();
    }

    public void Dispose()
    {
        if (File.Exists("test-appsettings.json"))
        {
            File.Delete("test-appsettings.json");
        }
    }
}
