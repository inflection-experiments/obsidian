using FluentAssertions;
using Moq;
using STLViewer.Application.Commands.Scene;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using Xunit;

namespace STLViewer.Application.Tests.Commands.Scene;

public class AddModelToSceneCommandTests
{
    private readonly Mock<ISceneManager> _mockSceneManager;
    private readonly AddModelToSceneCommandHandler _handler;

    public AddModelToSceneCommandTests()
    {
        _mockSceneManager = new Mock<ISceneManager>();
        _handler = new AddModelToSceneCommandHandler(_mockSceneManager.Object);
    }

    [Fact]
    public async Task Handle_ValidModel_ShouldAddToScene()
    {
        // Arrange
        var model = CreateTestModel();
        var command = new AddModelToSceneCommand(model, "Test Object");
        var sceneObject = SceneObject.Create("Test Object", model, Guid.NewGuid());

        _mockSceneManager.Setup(x => x.AddModelToScene(model, "Test Object", null, null))
            .Returns(Result<SceneObject>.Ok(sceneObject));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(sceneObject);

        _mockSceneManager.Verify(x => x.AddModelToScene(model, "Test Object", null, null), Times.Once);
    }

    [Fact]
    public async Task Handle_NullModel_ShouldFail()
    {
        // Arrange
        var command = new AddModelToSceneCommand(null!, "Test Object");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Model cannot be null");

        _mockSceneManager.Verify(x => x.AddModelToScene(It.IsAny<STLModel>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<SceneNode>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithParentId_ShouldFindParentAndAddToScene()
    {
        // Arrange
        var model = CreateTestModel();
        var parentNode = SceneGroup.Create("Parent Group", Guid.NewGuid());
        var parentId = parentNode.Id; // Use the actual parent node's ID
        var command = new AddModelToSceneCommand(model, "Test Object", null, parentId);
        var sceneObject = SceneObject.Create("Test Object", model, Guid.NewGuid());
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");

        _mockSceneManager.Setup(x => x.CurrentScene)
            .Returns(scene);

        scene.AddRootNode(parentNode);

        _mockSceneManager.Setup(x => x.AddModelToScene(model, "Test Object", null, parentNode))
            .Returns(Result<SceneObject>.Ok(sceneObject));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(sceneObject);

        _mockSceneManager.Verify(x => x.AddModelToScene(model, "Test Object", null, parentNode), Times.Once);
    }

    [Fact]
    public async Task Handle_WithParentId_NoActiveScene_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel();
        var parentId = Guid.NewGuid();
        var command = new AddModelToSceneCommand(model, "Test Object", null, parentId);

        _mockSceneManager.Setup(x => x.CurrentScene)
            .Returns((STLViewer.Domain.Entities.Scene?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No active scene");

        _mockSceneManager.Verify(x => x.AddModelToScene(It.IsAny<STLModel>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<SceneNode>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidParentId_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel();
        var parentId = Guid.NewGuid();
        var command = new AddModelToSceneCommand(model, "Test Object", null, parentId);
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");

        _mockSceneManager.Setup(x => x.CurrentScene)
            .Returns(scene);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Parent node with ID");
        result.Error.Should().Contain("not found");

        _mockSceneManager.Verify(x => x.AddModelToScene(It.IsAny<STLModel>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<SceneNode>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SceneManagerAddFails_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel();
        var command = new AddModelToSceneCommand(model, "Test Object");

        _mockSceneManager.Setup(x => x.AddModelToScene(model, "Test Object", null, null))
            .Returns(Result<SceneObject>.Fail("Failed to add model"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to add model");
    }

    [Fact]
    public async Task Handle_SceneManagerThrowsException_ShouldFail()
    {
        // Arrange
        var model = CreateTestModel();
        var command = new AddModelToSceneCommand(model, "Test Object");

        _mockSceneManager.Setup(x => x.AddModelToScene(model, "Test Object", null, null))
            .Throws(new InvalidOperationException("Scene manager error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to add model to scene");
        result.Error.Should().Contain("Scene manager error");
    }

    [Fact]
    public void Constructor_NullSceneManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AddModelToSceneCommandHandler(null!));
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
