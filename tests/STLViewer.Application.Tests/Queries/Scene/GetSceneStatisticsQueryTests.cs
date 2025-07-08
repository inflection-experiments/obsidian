using FluentAssertions;
using Moq;
using STLViewer.Application.Queries.Scene;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using Xunit;

namespace STLViewer.Application.Tests.Queries.Scene;

public class GetSceneStatisticsQueryTests
{
    private readonly Mock<ISceneManager> _mockSceneManager;
    private readonly GetSceneStatisticsQueryHandler _handler;

    public GetSceneStatisticsQueryTests()
    {
        _mockSceneManager = new Mock<ISceneManager>();
        _handler = new GetSceneStatisticsQueryHandler(_mockSceneManager.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ShouldReturnStatistics()
    {
        // Arrange
        var query = new GetSceneStatisticsQuery();
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        var statistics = SceneStatistics.Calculate(scene);

        _mockSceneManager.Setup(x => x.GetSceneStatistics())
            .Returns(Result<SceneStatistics>.Ok(statistics));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(statistics);
        result.Value.TotalTriangles.Should().Be(0);
        result.Value.TotalVertices.Should().Be(0);
        result.Value.TotalObjects.Should().Be(0);
        result.Value.TotalGroups.Should().Be(0);
        result.Value.LayerCount.Should().Be(1); // Default layer
        result.Value.EstimatedMemoryUsage.Should().Be(0);
        result.Value.VisibleObjects.Should().Be(0);
        result.Value.HiddenObjects.Should().Be(0);

        _mockSceneManager.Verify(x => x.GetSceneStatistics(), Times.Once);
    }

    [Fact]
    public async Task Handle_SceneManagerFails_ShouldFail()
    {
        // Arrange
        var query = new GetSceneStatisticsQuery();

        _mockSceneManager.Setup(x => x.GetSceneStatistics())
            .Returns(Result<SceneStatistics>.Fail("No active scene"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");

        _mockSceneManager.Verify(x => x.GetSceneStatistics(), Times.Once);
    }

    [Fact]
    public async Task Handle_SceneManagerThrowsException_ShouldFail()
    {
        // Arrange
        var query = new GetSceneStatisticsQuery();

        _mockSceneManager.Setup(x => x.GetSceneStatistics())
            .Throws(new InvalidOperationException("Statistics calculation error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to get scene statistics");
        result.Error.Should().Contain("Statistics calculation error");
    }

    [Fact]
    public void Constructor_NullSceneManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GetSceneStatisticsQueryHandler(null!));
    }
}
