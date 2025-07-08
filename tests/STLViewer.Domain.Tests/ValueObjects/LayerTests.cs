using FluentAssertions;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;
using Xunit;

namespace STLViewer.Domain.Tests.ValueObjects;

public class LayerTests
{
    [Fact]
    public void Create_ValidName_ShouldCreateLayer()
    {
        // Arrange
        var name = "Test Layer";
        var description = "Test Description";

        // Act
        var layer = Layer.Create(name, description);

        // Assert
        layer.Should().NotBeNull();
        layer.Name.Should().Be(name);
        layer.Description.Should().Be(description);
        layer.Id.Should().NotBe(Guid.Empty);
        layer.IsVisible.Should().BeTrue();
        layer.IsSelectable.Should().BeTrue();
        layer.IsLocked.Should().BeFalse();
        layer.IsDefault.Should().BeFalse();
        layer.Opacity.Should().Be(1.0f);
        layer.Order.Should().Be(0);
        layer.Color.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithColor_ShouldCreateLayerWithColor()
    {
        // Arrange
        var name = "Test Layer";
        var description = "Test Description";
        var color = new Color(1.0f, 0.0f, 0.0f, 1.0f);

        // Act
        var layer = Layer.Create(name, description, color);

        // Assert
        layer.Should().NotBeNull();
        layer.Name.Should().Be(name);
        layer.Description.Should().Be(description);
        layer.Color.Should().Be(color);
    }

    [Fact]
    public void Create_EmptyName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Layer.Create("", "Description"));
    }

    [Fact]
    public void Create_NullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Layer.Create(null!, "Description"));
    }

    [Fact]
    public void CreateDefault_ShouldCreateDefaultLayer()
    {
        // Act
        var layer = Layer.CreateDefault();

        // Assert
        layer.Should().NotBeNull();
        layer.Name.Should().Be("Default");
        layer.Description.Should().Be("Default layer for all objects");
        layer.IsDefault.Should().BeTrue();
        layer.IsVisible.Should().BeTrue();
        layer.IsSelectable.Should().BeTrue();
        layer.IsLocked.Should().BeFalse();
        layer.Opacity.Should().Be(1.0f);
        layer.Order.Should().Be(0);
    }

    [Fact]
    public void With_UpdateName_ShouldReturnNewInstanceWithUpdatedName()
    {
        // Arrange
        var originalLayer = Layer.Create("Original", "Original Description");
        var newName = "Updated Name";

        // Act
        var updatedLayer = originalLayer.With(name: newName);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.Name.Should().Be(newName);
        updatedLayer.Description.Should().Be(originalLayer.Description);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateDescription_ShouldReturnNewInstanceWithUpdatedDescription()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Original Description");
        var newDescription = "Updated Description";

        // Act
        var updatedLayer = originalLayer.With(description: newDescription);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Description.Should().Be(newDescription);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateColor_ShouldReturnNewInstanceWithUpdatedColor()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Description");
        var newColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);

        // Act
        var updatedLayer = originalLayer.With(color: newColor);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.Color.Should().Be(newColor);
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateVisibility_ShouldReturnNewInstanceWithUpdatedVisibility()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Description");

        // Act
        var updatedLayer = originalLayer.With(isVisible: false);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.IsVisible.Should().BeFalse();
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateSelectability_ShouldReturnNewInstanceWithUpdatedSelectability()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Description");

        // Act
        var updatedLayer = originalLayer.With(isSelectable: false);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.IsSelectable.Should().BeFalse();
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateLocked_ShouldReturnNewInstanceWithUpdatedLocked()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Description");

        // Act
        var updatedLayer = originalLayer.With(isLocked: true);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.IsLocked.Should().BeTrue();
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateOpacity_ShouldReturnNewInstanceWithUpdatedOpacity()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Description");
        var newOpacity = 0.5f;

        // Act
        var updatedLayer = originalLayer.With(opacity: newOpacity);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.Opacity.Should().Be(newOpacity);
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_UpdateOrder_ShouldReturnNewInstanceWithUpdatedOrder()
    {
        // Arrange
        var originalLayer = Layer.Create("Name", "Description");
        var newOrder = 10;

        // Act
        var updatedLayer = originalLayer.With(order: newOrder);

        // Assert
        updatedLayer.Should().NotBe(originalLayer);
        updatedLayer.Order.Should().Be(newOrder);
        updatedLayer.Name.Should().Be(originalLayer.Name);
        updatedLayer.Id.Should().Be(originalLayer.Id);
    }

    [Fact]
    public void With_InvalidOpacity_ShouldThrowArgumentException()
    {
        // Arrange
        var layer = Layer.Create("Name", "Description");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => layer.With(opacity: -0.1f));
        Assert.Throws<ArgumentException>(() => layer.With(opacity: 1.1f));
    }

    [Fact]
    public void With_EmptyName_ShouldAllowEmptyName()
    {
        // Arrange
        var layer = Layer.Create("Name", "Description");

        // Act
        var updatedLayer = layer.With(name: "");

        // Assert
        updatedLayer.Name.Should().Be("");
    }

    [Fact]
    public void With_NullName_ShouldKeepOriginalName()
    {
        // Arrange
        var layer = Layer.Create("Name", "Description");

        // Act
        var updatedLayer = layer.With(name: null);

        // Assert
        updatedLayer.Name.Should().Be("Name");
    }

    [Fact]
    public void Equality_SameLayers_ShouldBeEqual()
    {
        // Arrange
        var layer1 = Layer.Create("Name", "Description");
        var layer2 = layer1;

        // Act & Assert
        layer1.Should().Be(layer2);
        layer1.Equals(layer2).Should().BeTrue();
        (layer1 == layer2).Should().BeTrue();
        (layer1 != layer2).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentLayers_ShouldNotBeEqual()
    {
        // Arrange
        var layer1 = Layer.Create("Name1", "Description1");
        var layer2 = Layer.Create("Name2", "Description2");

        // Act & Assert
        layer1.Should().NotBe(layer2);
        layer1.Equals(layer2).Should().BeFalse();
        (layer1 == layer2).Should().BeFalse();
        (layer1 != layer2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameLayers_ShouldHaveSameHashCode()
    {
        // Arrange
        var layer1 = Layer.Create("Name", "Description");
        var layer2 = layer1;

        // Act & Assert
        layer1.GetHashCode().Should().Be(layer2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldIncludeAllProperties()
    {
        // Arrange
        var name = "Test Layer";
        var layer = Layer.Create(name, "Description");

        // Act
        var result = layer.ToString();

        // Assert
        result.Should().Contain(name);
        result.Should().Contain("Description");
        result.Should().Contain("IsVisible");
    }
}
