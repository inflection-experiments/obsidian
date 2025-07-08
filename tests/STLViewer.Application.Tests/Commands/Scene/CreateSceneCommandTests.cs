using FluentAssertions;
using Moq;
using STLViewer.Application.Commands.Scene;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using Xunit;

namespace STLViewer.Application.Tests.Commands.Scene;

public class CreateSceneCommandTests
{
    private readonly Mock<ISceneManager> _mockSceneManager;
    private readonly Mock<ISceneRepository> _mockSceneRepository;
    private readonly CreateSceneCommandHandler _handler;

    public CreateSceneCommandTests()
    {
        _mockSceneManager = new Mock<ISceneManager>();
        _mockSceneRepository = new Mock<ISceneRepository>();
        _handler = new CreateSceneCommandHandler(_mockSceneManager.Object, _mockSceneRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidSceneName_ShouldCreateScene()
    {
        // Arrange
        var command = new CreateSceneCommand("Test Scene", "Test Description");
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");

        _mockSceneManager.Setup(x => x.CreateScene("Test Scene"))
            .Returns(Result<STLViewer.Domain.Entities.Scene>.Ok(scene));

        _mockSceneRepository.Setup(x => x.SaveAsync(It.IsAny<STLViewer.Domain.Entities.Scene>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Scene");
        result.Value.Description.Should().Be("Test Description");

        _mockSceneManager.Verify(x => x.CreateScene("Test Scene"), Times.Once);
        _mockSceneRepository.Verify(x => x.SaveAsync(It.IsAny<STLViewer.Domain.Entities.Scene>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptySceneName_ShouldFail()
    {
        // Arrange
        var command = new CreateSceneCommand("", "Test Description");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Scene name cannot be null or empty");

        _mockSceneManager.Verify(x => x.CreateScene(It.IsAny<string>()), Times.Never);
        _mockSceneRepository.Verify(x => x.SaveAsync(It.IsAny<STLViewer.Domain.Entities.Scene>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SceneManagerCreateFails_ShouldFail()
    {
        // Arrange
        var command = new CreateSceneCommand("Test Scene", "Test Description");

        _mockSceneManager.Setup(x => x.CreateScene("Test Scene"))
            .Returns(Result<STLViewer.Domain.Entities.Scene>.Fail("Scene creation failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Scene creation failed");

        _mockSceneRepository.Verify(x => x.SaveAsync(It.IsAny<STLViewer.Domain.Entities.Scene>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RepositorySaveFails_ShouldFail()
    {
        // Arrange
        var command = new CreateSceneCommand("Test Scene", "Test Description");
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");

        _mockSceneManager.Setup(x => x.CreateScene("Test Scene"))
            .Returns(Result<STLViewer.Domain.Entities.Scene>.Ok(scene));

        _mockSceneRepository.Setup(x => x.SaveAsync(It.IsAny<STLViewer.Domain.Entities.Scene>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail("Save failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Save failed");
    }

    [Fact]
    public async Task Handle_SceneManagerThrowsException_ShouldFail()
    {
        // Arrange
        var command = new CreateSceneCommand("Test Scene", "Test Description");

        _mockSceneManager.Setup(x => x.CreateScene("Test Scene"))
            .Throws(new InvalidOperationException("Scene manager error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to create scene");
        result.Error.Should().Contain("Scene manager error");
    }

    [Fact]
    public void Constructor_NullSceneManager_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CreateSceneCommandHandler(null!, _mockSceneRepository.Object));
    }

    [Fact]
    public void Constructor_NullSceneRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CreateSceneCommandHandler(_mockSceneManager.Object, null!));
    }
}
