using FluentAssertions;
using Moq;
using STLViewer.Application.Commands.Scene;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using Xunit;

namespace STLViewer.Application.Tests.Commands.Scene;

public class CreateLayerCommandTests
{
    private readonly Mock<ISceneManager> _mockSceneManager;
    private readonly CreateLayerCommandHandler _handler;

    public CreateLayerCommandTests()
    {
        _mockSceneManager = new Mock<ISceneManager>();
        _handler = new CreateLayerCommandHandler(_mockSceneManager.Object);
    }

    [Fact]
    public async Task Handle_ValidLayerName_ShouldCreateLayer()
    {
        // Arrange
        var command = new CreateLayerCommand("Test Layer", "Test Description");
        var layer = Layer.Create("Test Layer", "Test Description");

        _mockSceneManager.Setup(x => x.CreateLayer("Test Layer", "Test Description", null))
            .Returns(Result<Layer>.Ok(layer));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Layer");
        result.Value.Description.Should().Be("Test Description");

        _mockSceneManager.Verify(x => x.CreateLayer("Test Layer", "Test Description", null), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyLayerName_ShouldFail()
    {
        // Arrange
        var command = new CreateLayerCommand("", "Test Description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Layer name cannot be null or empty");

        _mockSceneManager.Verify(x => x.CreateLayer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<STLViewer.Math.Color?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithCustomProperties_ShouldCreateAndUpdateLayer()
    {
        // Arrange
        var color = new STLViewer.Math.Color(1.0f, 0.0f, 0.0f, 1.0f);
        var command = new CreateLayerCommand("Test Layer", "Test Description", color, false, false, true, 0.5f);
        var layer = Layer.Create("Test Layer", "Test Description", color);

        _mockSceneManager.Setup(x => x.CreateLayer("Test Layer", "Test Description", color))
            .Returns(Result<Layer>.Ok(layer));

        _mockSceneManager.Setup(x => x.UpdateLayer(layer.Id, null, null, null, false, false, true, 0.5f))
            .Returns(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Layer");
        result.Value.Description.Should().Be("Test Description");
        result.Value.Color.Should().Be(color);

        _mockSceneManager.Verify(x => x.CreateLayer("Test Layer", "Test Description", color), Times.Once);
        _mockSceneManager.Verify(x => x.UpdateLayer(layer.Id, null, null, null, false, false, true, 0.5f), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDefaultProperties_ShouldCreateLayerWithoutUpdate()
    {
        // Arrange
        var command = new CreateLayerCommand("Test Layer", "Test Description", null, true, true, false, 1.0f);
        var layer = Layer.Create("Test Layer", "Test Description");

        _mockSceneManager.Setup(x => x.CreateLayer("Test Layer", "Test Description", null))
            .Returns(Result<Layer>.Ok(layer));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Layer");
        result.Value.Description.Should().Be("Test Description");

        _mockSceneManager.Verify(x => x.CreateLayer("Test Layer", "Test Description", null), Times.Once);
        _mockSceneManager.Verify(x => x.UpdateLayer(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<STLViewer.Math.Color?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SceneManagerCreateFails_ShouldFail()
    {
        // Arrange
        var command = new CreateLayerCommand("Test Layer", "Test Description");

        _mockSceneManager.Setup(x => x.CreateLayer("Test Layer", "Test Description", null))
            .Returns(Result<Layer>.Fail("Layer creation failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Layer creation failed");
    }

    [Fact]
    public async Task Handle_SceneManagerUpdateFails_ShouldFail()
    {
        // Arrange
        var command = new CreateLayerCommand("Test Layer", "Test Description", null, false, true, false, 1.0f);
        var layer = Layer.Create("Test Layer", "Test Description");

        _mockSceneManager.Setup(x => x.CreateLayer("Test Layer", "Test Description", null))
            .Returns(Result<Layer>.Ok(layer));

        _mockSceneManager.Setup(x => x.UpdateLayer(layer.Id, null, null, null, false, true, false, 1.0f))
            .Returns(Result.Fail("Update failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Update failed");
    }

    [Fact]
    public async Task Handle_SceneManagerThrowsException_ShouldFail()
    {
        // Arrange
        var command = new CreateLayerCommand("Test Layer", "Test Description");

        _mockSceneManager.Setup(x => x.CreateLayer("Test Layer", "Test Description", null))
            .Throws(new InvalidOperationException("Scene manager error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to create layer");
        result.Error.Should().Contain("Scene manager error");
    }

    [Fact]
    public void Constructor_NullSceneManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CreateLayerCommandHandler(null!));
    }
}
