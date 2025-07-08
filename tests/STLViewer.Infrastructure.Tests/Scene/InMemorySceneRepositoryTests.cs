using FluentAssertions;
using STLViewer.Infrastructure.Scene;
using Xunit;

namespace STLViewer.Infrastructure.Tests.Scene;

public class InMemorySceneRepositoryTests : IDisposable
{
    private readonly InMemorySceneRepository _repository;

    public InMemorySceneRepositoryTests()
    {
        _repository = new InMemorySceneRepository();
    }

    public void Dispose()
    {
        _repository.Clear();
    }

    [Fact]
    public async Task SaveAsync_ValidScene_ShouldSaveScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");

        // Act
        var result = await _repository.SaveAsync(scene);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Count.Should().Be(1);
        _repository.SceneIds.Should().Contain(scene.Id);
    }

    [Fact]
    public async Task SaveAsync_NullScene_ShouldFail()
    {
        // Act
        var result = await _repository.SaveAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Scene cannot be null");
        _repository.Count.Should().Be(0);
    }

    [Fact]
    public async Task SaveAsync_ExistingScene_ShouldUpdateScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        await _repository.SaveAsync(scene);
        scene.Description = "Updated Description";

        // Act
        var result = await _repository.SaveAsync(scene);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Count.Should().Be(1);

        var loadResult = await _repository.LoadAsync(scene.Id);
        loadResult.IsSuccess.Should().BeTrue();
        loadResult.Value.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task LoadAsync_ExistingScene_ShouldReturnScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        await _repository.SaveAsync(scene);

        // Act
        var result = await _repository.LoadAsync(scene.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(scene);
        result.Value.Name.Should().Be("Test Scene");
    }

    [Fact]
    public async Task LoadAsync_NonExistingScene_ShouldFail()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.LoadAsync(nonExistingId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteAsync_ExistingScene_ShouldDeleteScene()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        await _repository.SaveAsync(scene);

        // Act
        var result = await _repository.DeleteAsync(scene.Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Count.Should().Be(0);
        _repository.SceneIds.Should().NotContain(scene.Id);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingScene_ShouldFail()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistingId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetAllAsync_EmptyRepository_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithScenes_ShouldReturnAllScenes()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");
        await _repository.SaveAsync(scene1);
        await _repository.SaveAsync(scene2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(scene1);
        result.Value.Should().Contain(scene2);
    }

    [Fact]
    public async Task GetMetadataAsync_WithScenes_ShouldReturnMetadata()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");
        await _repository.SaveAsync(scene1);
        await _repository.SaveAsync(scene2);

        // Act
        var result = await _repository.GetMetadataAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        var metadata = result.Value.ToList();
        metadata.Should().Contain(m => m.Name == "Scene 1");
        metadata.Should().Contain(m => m.Name == "Scene 2");
    }

    [Fact]
    public async Task ExistsAsync_ExistingScene_ShouldReturnTrue()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        await _repository.SaveAsync(scene);

        // Act
        var result = await _repository.ExistsAsync(scene.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingScene_ShouldReturnFalse()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountAsync_EmptyRepository_ShouldReturnZero()
    {
        // Act
        var result = await _repository.GetCountAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task GetCountAsync_WithScenes_ShouldReturnCorrectCount()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");
        await _repository.SaveAsync(scene1);
        await _repository.SaveAsync(scene2);

        // Act
        var result = await _repository.GetCountAsync();

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task SearchByNameAsync_EmptySearchTerm_ShouldFail()
    {
        // Act
        var result = await _repository.SearchByNameAsync("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Search term cannot be null or empty");
    }

    [Fact]
    public async Task SearchByNameAsync_ValidSearchTerm_ShouldReturnMatchingScenes()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Test Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Another Scene");
        var scene3 = STLViewer.Domain.Entities.Scene.Create("Test Scene 2");
        await _repository.SaveAsync(scene1);
        await _repository.SaveAsync(scene2);
        await _repository.SaveAsync(scene3);

        // Act
        var result = await _repository.SearchByNameAsync("Test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(s => s.Name == "Test Scene 1");
        result.Value.Should().Contain(s => s.Name == "Test Scene 2");
        result.Value.Should().NotContain(s => s.Name == "Another Scene");
    }

    [Fact]
    public async Task SearchByNameAsync_CaseInsensitive_ShouldReturnMatchingScenes()
    {
        // Arrange
        var scene = STLViewer.Domain.Entities.Scene.Create("Test Scene");
        await _repository.SaveAsync(scene);

        // Act
        var result = await _repository.SearchByNameAsync("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Should().Contain(scene);
    }

    [Fact]
    public async Task GetRecentlyModifiedAsync_WithScenes_ShouldReturnInDescendingOrder()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");
        var scene3 = STLViewer.Domain.Entities.Scene.Create("Scene 3");

        await _repository.SaveAsync(scene1);
        await Task.Delay(10); // Small delay to ensure different timestamps
        await _repository.SaveAsync(scene2);
        await Task.Delay(10);
        await _repository.SaveAsync(scene3);

        // Act
        var result = await _repository.GetRecentlyModifiedAsync(2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        var scenes = result.Value.ToList();
        scenes[0].Should().Be(scene3); // Most recent
        scenes[1].Should().Be(scene2); // Second most recent
    }

    [Fact]
    public void Clear_WithScenes_ShouldClearAllScenes()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");
        _repository.SaveAsync(scene1).Wait();
        _repository.SaveAsync(scene2).Wait();

        // Act
        _repository.Clear();

        // Assert
        _repository.Count.Should().Be(0);
        _repository.SceneIds.Should().BeEmpty();
    }

    [Fact]
    public void Count_Property_ShouldReturnCorrectCount()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");

        // Act & Assert
        _repository.Count.Should().Be(0);

        _repository.SaveAsync(scene1).Wait();
        _repository.Count.Should().Be(1);

        _repository.SaveAsync(scene2).Wait();
        _repository.Count.Should().Be(2);

        _repository.DeleteAsync(scene1.Id).Wait();
        _repository.Count.Should().Be(1);
    }

    [Fact]
    public void SceneIds_Property_ShouldReturnAllSceneIds()
    {
        // Arrange
        var scene1 = STLViewer.Domain.Entities.Scene.Create("Scene 1");
        var scene2 = STLViewer.Domain.Entities.Scene.Create("Scene 2");
        _repository.SaveAsync(scene1).Wait();
        _repository.SaveAsync(scene2).Wait();

        // Act
        var sceneIds = _repository.SceneIds.ToList();

        // Assert
        sceneIds.Should().HaveCount(2);
        sceneIds.Should().Contain(scene1.Id);
        sceneIds.Should().Contain(scene2.Id);
    }
}
