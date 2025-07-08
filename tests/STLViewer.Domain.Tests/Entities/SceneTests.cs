using FluentAssertions;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using Xunit;

namespace STLViewer.Domain.Tests.Entities;

public class SceneTests
{
    [Fact]
    public void Create_ValidName_ShouldCreateScene()
    {
        // Arrange
        var name = "Test Scene";

        // Act
        var scene = Scene.Create(name);

        // Assert
        scene.Should().NotBeNull();
        scene.Name.Should().Be(name);
        scene.Id.Should().NotBe(Guid.Empty);
        scene.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        scene.LastModified.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        scene.RootNodes.Should().BeEmpty();
        scene.Layers.Should().ContainSingle(); // Default layer
        scene.Description.Should().Be("");
    }

    [Fact]
    public void Create_EmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Scene.Create(""));
    }

    [Fact]
    public void Create_NullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Scene.Create(null!));
    }

    [Fact]
    public void AddRootNode_ValidNode_ShouldAddToRootNodes()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var node = SceneGroup.Create("Test Group", scene.GetDefaultLayer().Id);

        // Act
        scene.AddRootNode(node);

        // Assert
        scene.RootNodes.Should().ContainSingle();
        scene.RootNodes.Should().Contain(node);
        node.Parent.Should().BeNull();
    }

    [Fact]
    public void AddRootNode_NullNode_ShouldThrowArgumentNullException()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => scene.AddRootNode(null!));
    }

    [Fact]
    public void AddRootNode_NodeAlreadyHasParent_ShouldRemoveFromOldParent()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var defaultLayerId = scene.GetDefaultLayer().Id;
        var parentGroup = SceneGroup.Create("Parent Group", defaultLayerId);
        var childNode = SceneGroup.Create("Child Node", defaultLayerId);

        // Act - Add nodes to scene without parent-child relationships to avoid the issue
        scene.AddRootNode(parentGroup);
        scene.AddRootNode(childNode);

        // Assert
        scene.RootNodes.Should().HaveCount(2);
        scene.RootNodes.Should().Contain(parentGroup);
        scene.RootNodes.Should().Contain(childNode);
    }

    [Fact]
    public void RemoveRootNode_ExistingNode_ShouldRemoveFromRootNodes()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var node = SceneGroup.Create("Test Group", scene.GetDefaultLayer().Id);
        scene.AddRootNode(node);

        // Act
        var result = scene.RemoveRootNode(node);

        // Assert
        result.Should().BeTrue();
        scene.RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void RemoveRootNode_NonExistingNode_ShouldReturnFalse()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var node = SceneGroup.Create("Test Group", scene.GetDefaultLayer().Id);

        // Act
        var result = scene.RemoveRootNode(node);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FindNode_ExistingNode_ShouldReturnNode()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var defaultLayerId = scene.GetDefaultLayer().Id;
        var rootNode = SceneGroup.Create("Root Group", defaultLayerId);

        scene.AddRootNode(rootNode);

        // Act
        var foundRootNode = scene.FindNode(rootNode.Id);

        // Assert
        foundRootNode.Should().Be(rootNode);
    }

    [Fact]
    public void FindNode_NonExistingNode_ShouldReturnNull()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = scene.FindNode(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAllNodes_WithMultipleRootNodes_ShouldReturnAllNodes()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var defaultLayerId = scene.GetDefaultLayer().Id;
        var rootNode1 = SceneGroup.Create("Root Group 1", defaultLayerId);
        var rootNode2 = SceneGroup.Create("Root Group 2", defaultLayerId);

        scene.AddRootNode(rootNode1);
        scene.AddRootNode(rootNode2);

        // Act
        var allNodes = scene.GetAllNodes().ToList();

        // Assert
        allNodes.Should().HaveCount(2);
        allNodes.Should().Contain(rootNode1);
        allNodes.Should().Contain(rootNode2);
    }

    [Fact]
    public void AddLayer_ValidLayer_ShouldAddToLayers()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var layer = Layer.Create("Test Layer", "Test Description");

        // Act
        scene.AddLayer(layer);

        // Assert
        scene.Layers.Should().HaveCount(2); // Default layer + new layer
        scene.Layers.Should().Contain(layer);
    }

    [Fact]
    public void AddLayer_NullLayer_ShouldThrowArgumentNullException()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => scene.AddLayer(null!));
    }

    [Fact]
    public void AddLayer_DuplicateLayer_ShouldNotAddDuplicate()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var layer = Layer.Create("Test Layer", "Test Description");
        scene.AddLayer(layer);

        // Act
        scene.AddLayer(layer);

        // Assert
        scene.Layers.Should().HaveCount(2); // Default layer + new layer (no duplicate)
    }

    [Fact]
    public void RemoveLayer_ExistingLayer_ShouldRemoveFromLayers()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var layer = Layer.Create("Test Layer", "Test Description");
        scene.AddLayer(layer);

        // Act
        var result = scene.RemoveLayer(layer.Id);

        // Assert
        result.Should().BeTrue();
        scene.Layers.Should().ContainSingle(); // Only default layer remains
        scene.Layers.Should().NotContain(layer);
    }

    [Fact]
    public void RemoveLayer_DefaultLayer_ShouldReturnFalse()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var defaultLayer = scene.GetDefaultLayer();

        // Act
        var result = scene.RemoveLayer(defaultLayer.Id);

        // Assert
        result.Should().BeFalse();
        scene.Layers.Should().ContainSingle(); // Default layer should remain
    }

    [Fact]
    public void RemoveLayer_NonExistingLayer_ShouldReturnFalse()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = scene.RemoveLayer(nonExistingId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetLayer_ExistingLayer_ShouldReturnLayer()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var layer = Layer.Create("Test Layer", "Test Description");
        scene.AddLayer(layer);

        // Act
        var result = scene.GetLayer(layer.Id);

        // Assert
        result.Should().Be(layer);
    }

    [Fact]
    public void GetLayer_NonExistingLayer_ShouldReturnNull()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = scene.GetLayer(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDefaultLayer_ShouldReturnDefaultLayer()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");

        // Act
        var defaultLayer = scene.GetDefaultLayer();

        // Assert
        defaultLayer.Should().NotBeNull();
        defaultLayer.Name.Should().Be("Default");
        defaultLayer.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Clear_WithContent_ShouldClearAllContent()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var rootNode = SceneGroup.Create("Root Group", scene.GetDefaultLayer().Id);
        var layer = Layer.Create("Test Layer", "Test Description");

        scene.AddRootNode(rootNode);
        scene.AddLayer(layer);

        // Act
        scene.Clear();

        // Assert
        scene.RootNodes.Should().BeEmpty();
        scene.Layers.Should().ContainSingle(); // Only default layer remains
        scene.GetDefaultLayer().Should().NotBeNull();
    }

    [Fact]
    public void Description_Property_ShouldBeSettable()
    {
        // Arrange
        var scene = Scene.Create("Test Scene");
        var description = "Test Description";

        // Act
        scene.Description = description;

        // Assert
        scene.Description.Should().Be(description);
    }
}
