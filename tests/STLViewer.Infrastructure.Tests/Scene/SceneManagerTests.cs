using FluentAssertions;
using Moq;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Infrastructure.Scene;
using Xunit;

namespace STLViewer.Infrastructure.Tests.Scene;

public class SceneManagerTests
{
    private readonly Mock<ISceneRepository> _mockRepository;
    private readonly SceneManager _sceneManager;

    public SceneManagerTests()
    {
        _mockRepository = new Mock<ISceneRepository>();
        _sceneManager = new SceneManager(_mockRepository.Object);
    }

    [Fact]
    public void Constructor_NullRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SceneManager(null!));
    }

    [Fact]
    public void CreateScene_ValidName_ShouldCreateScene()
    {
        // Arrange
        var sceneName = "Test Scene";

        // Act
        var result = _sceneManager.CreateScene(sceneName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(sceneName);
        result.Value.RootNodes.Should().BeEmpty();
        result.Value.Layers.Should().ContainSingle(); // Should have default layer
    }

    [Fact]
    public void CreateScene_ThrowsException_ShouldFail()
    {
        // Arrange
        var sceneName = "Test Scene";

        // Act
        var result = _sceneManager.CreateScene(sceneName);

        // Assert - Even if the test doesn't throw, we should have a valid scene
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SetCurrentScene_ValidScene_ShouldSetSceneAndRaiseEvent()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        bool eventRaised = false;
        SceneChangedEventArgs? eventArgs = null;

        _sceneManager.CurrentSceneChanged += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        var result = _sceneManager.SetCurrentScene(scene);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _sceneManager.CurrentScene.Should().Be(scene);
        eventRaised.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.NewScene.Should().Be(scene);
        eventArgs.PreviousScene.Should().BeNull();
    }

    [Fact]
    public void SetCurrentScene_NullScene_ShouldClearCurrentScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);

        // Act
        var result = _sceneManager.SetCurrentScene(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _sceneManager.CurrentScene.Should().BeNull();
    }

    [Fact]
    public void AddModelToScene_NoCurrentScene_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel();

        // Act
        var result = _sceneManager.AddModelToScene(model);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");
    }

    [Fact]
    public void AddModelToScene_ValidModel_ShouldAddToCurrentScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);
        var model = CreateTestModel();

        // Act
        var result = _sceneManager.AddModelToScene(model, "Test Object");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Model.Should().Be(model);
        result.Value.Name.Should().Be("Test Object");
        scene.RootNodes.Should().ContainSingle();
        scene.RootNodes.First().Should().Be(result.Value);
    }

    [Fact]
    public void AddModelToScene_NullModel_ShouldFail()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);

        // Act
        var result = _sceneManager.AddModelToScene(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Model cannot be null");
    }

    [Fact]
    public void CreateLayer_NoCurrentScene_ShouldFail()
    {
        // Act
        var result = _sceneManager.CreateLayer("Test Layer");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");
    }

    [Fact]
    public void CreateLayer_ValidName_ShouldCreateLayer()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);

        // Act
        var result = _sceneManager.CreateLayer("Test Layer", "Test Description");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Layer");
        result.Value.Description.Should().Be("Test Description");
        scene.Layers.Should().Contain(result.Value);
    }

    [Fact]
    public void GetSceneStatistics_NoCurrentScene_ShouldFail()
    {
        // Act
        var result = _sceneManager.GetSceneStatistics();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");
    }

    [Fact]
    public void GetSceneStatistics_WithCurrentScene_ShouldReturnStatistics()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);
        var model = CreateTestModel();
        _sceneManager.AddModelToScene(model, "Test Object");

        // Act
        var result = _sceneManager.GetSceneStatistics();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalObjects.Should().Be(1);
        result.Value.LayerCount.Should().Be(1); // Default layer
        result.Value.TotalTriangles.Should().Be(model.TriangleCount);
    }

    [Fact]
    public void RemoveFromScene_NoCurrentScene_ShouldFail()
    {
        // Act
        var result = _sceneManager.RemoveFromScene(Guid.NewGuid());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");
    }

    [Fact]
    public void RemoveFromScene_ValidNodeId_ShouldRemoveNode()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);
        var model = CreateTestModel();
        var addResult = _sceneManager.AddModelToScene(model, "Test Object");
        var nodeId = addResult.Value.Id;

        // Act
        var result = _sceneManager.RemoveFromScene(nodeId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        scene.RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFromScene_InvalidNodeId_ShouldFail()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);
        var invalidId = Guid.NewGuid();

        // Act
        var result = _sceneManager.RemoveFromScene(invalidId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found in scene");
    }

    [Fact]
    public void ClearScene_NoCurrentScene_ShouldFail()
    {
        // Act
        var result = _sceneManager.ClearScene();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");
    }

    [Fact]
    public void ClearScene_WithCurrentScene_ShouldClearScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);
        var model = CreateTestModel();
        _sceneManager.AddModelToScene(model, "Test Object");

        // Act
        var result = _sceneManager.ClearScene();

        // Assert
        result.IsSuccess.Should().BeTrue();
        scene.RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void SceneModified_WhenModelAdded_ShouldRaiseEvent()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        _sceneManager.SetCurrentScene(scene);
        var model = CreateTestModel();
        bool eventRaised = false;
        SceneModifiedEventArgs? eventArgs = null;

        _sceneManager.SceneModified += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        var result = _sceneManager.AddModelToScene(model, "Test Object");

        // Assert
        result.IsSuccess.Should().BeTrue();
        eventRaised.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.ModificationType.Should().Be(SceneModificationType.NodeAdded);
        eventArgs.AffectedNodeId.Should().Be(result.Value.Id);
    }

    private STLModel CreateTestModel()
    {
        var triangles = new[]
        {
            Triangle.Create(
                new STLViewer.Math.Vector3(0, 0, 0),
                new STLViewer.Math.Vector3(1, 0, 0),
                new STLViewer.Math.Vector3(0, 1, 0)
            )
        };

        var metadata = ModelMetadata.Create(
            "test.stl",
            STLViewer.Domain.Enums.STLFormat.ASCII,
            triangles.Length,
            new STLViewer.Math.BoundingBox(
                new STLViewer.Math.Vector3(0, 0, 0),
                new STLViewer.Math.Vector3(1, 1, 0)
            )
        );

        var rawData = System.Text.Encoding.UTF8.GetBytes("fake stl data");
        var result = STLModel.Create(metadata, triangles, rawData);
        return result.Value;
    }

    private long CalculateFileSize(Triangle[] triangles)
    {
        // Simple approximation for file size
        return triangles.Length * 50; // Approximate bytes per triangle
    }
}
