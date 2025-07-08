using FluentAssertions;
using STLViewer.Core.Interfaces;
using STLViewer.Math;
using Xunit;

namespace STLViewer.Core.Tests.Rendering;

/// <summary>
/// Tests for the RenderSettings class and material integration.
/// </summary>
public class RenderSettingsTests
{
    [Fact]
    public void RenderSettings_DefaultConstructor_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var settings = new RenderSettings();

        // Assert
        settings.RenderMode.Should().Be(RenderMode.Surface);
        settings.Wireframe.Should().BeFalse();
        settings.Shading.Should().Be(ShadingMode.Smooth);
        settings.Material.Should().NotBeNull();
        settings.BackgroundColor.Should().Be(Color.DarkGray);
        settings.ShowNormals.Should().BeFalse();
        settings.ShowBoundingBox.Should().BeFalse();
        settings.AntiAliasing.Should().Be(4);
        settings.DepthTesting.Should().BeTrue();
        settings.BackfaceCulling.Should().BeTrue();
        settings.EnableTransparency.Should().BeFalse();
    }

    [Fact]
    public void RenderSettings_Material_ShouldInitializeWithDefaultMaterial()
    {
        // Arrange & Act
        var settings = new RenderSettings();

        // Assert
        settings.Material.Should().NotBeNull();
        settings.Material.Preset.Should().Be(MaterialPreset.Default);
        settings.Material.DiffuseColor.Should().Be(Color.LightGray);
    }

    [Fact]
    public void RenderSettings_ModelColor_ShouldMapToMaterialDiffuseColor()
    {
        // Arrange
        var settings = new RenderSettings();
        var newColor = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Red

        // Act
        settings.ModelColor = newColor;

        // Assert
        settings.Material.DiffuseColor.Should().Be(newColor);
        settings.ModelColor.Should().Be(newColor);
    }

    [Fact]
    public void RenderSettings_MaterialDiffuseColor_ShouldReflectInModelColor()
    {
        // Arrange
        var settings = new RenderSettings();
        var newColor = new Color(0.0f, 1.0f, 0.0f, 1.0f); // Green

        // Act
        settings.Material.DiffuseColor = newColor;

        // Assert
        settings.ModelColor.Should().Be(newColor);
    }

    [Fact]
    public void RenderSettings_SetMaterial_ShouldUpdateMaterialReference()
    {
        // Arrange
        var settings = new RenderSettings();
        var newMaterial = Material.FromPreset(MaterialPreset.Metal, new Color(0.8f, 0.8f, 0.8f));

        // Act
        settings.Material = newMaterial;

        // Assert
        settings.Material.Should().BeSameAs(newMaterial);
        settings.Material.Preset.Should().Be(MaterialPreset.Metal);
        settings.ModelColor.Should().Be(newMaterial.DiffuseColor);
    }

    [Theory]
    [InlineData(RenderMode.Surface)]
    [InlineData(RenderMode.Wireframe)]
    [InlineData(RenderMode.ShadedWireframe)]
    public void RenderSettings_RenderMode_ShouldAcceptValidModes(RenderMode mode)
    {
        // Arrange
        var settings = new RenderSettings();

        // Act
        settings.RenderMode = mode;

        // Assert
        settings.RenderMode.Should().Be(mode);
    }

    [Theory]
    [InlineData(ShadingMode.Flat)]
    [InlineData(ShadingMode.Smooth)]
    public void RenderSettings_ShadingMode_ShouldAcceptValidModes(ShadingMode mode)
    {
        // Arrange
        var settings = new RenderSettings();

        // Act
        settings.Shading = mode;

        // Assert
        settings.Shading.Should().Be(mode);
    }

    [Fact]
    public void RenderSettings_EnableTransparency_WhenTrue_ShouldSupportAlphaBlending()
    {
        // Arrange
        var settings = new RenderSettings();
        settings.Material.Alpha = 0.5f;

        // Act
        settings.EnableTransparency = true;

        // Assert
        settings.EnableTransparency.Should().BeTrue();
        settings.Material.Alpha.Should().Be(0.5f);
    }

    [Fact]
    public void RenderSettings_Lighting_ShouldHaveDefaultConfiguration()
    {
        // Arrange & Act
        var settings = new RenderSettings();

        // Assert
        settings.Lighting.Should().NotBeNull();
        settings.Lighting.Enabled.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void RenderSettings_AntiAliasing_ShouldAcceptValidLevels(int level)
    {
        // Arrange
        var settings = new RenderSettings();

        // Act
        settings.AntiAliasing = level;

        // Assert
        settings.AntiAliasing.Should().Be(level);
    }

    [Fact]
    public void RenderSettings_BackgroundColor_ShouldBeSettable()
    {
        // Arrange
        var settings = new RenderSettings();
        var newColor = new Color(0.1f, 0.2f, 0.3f, 1.0f);

        // Act
        settings.BackgroundColor = newColor;

        // Assert
        settings.BackgroundColor.Should().Be(newColor);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RenderSettings_BooleanProperties_ShouldBeSettable(bool value)
    {
        // Arrange
        var settings = new RenderSettings();

        // Act & Assert
        settings.Wireframe = value;
        settings.Wireframe.Should().Be(value);

        settings.ShowNormals = value;
        settings.ShowNormals.Should().Be(value);

        settings.ShowBoundingBox = value;
        settings.ShowBoundingBox.Should().Be(value);

        settings.DepthTesting = value;
        settings.DepthTesting.Should().Be(value);

        settings.BackfaceCulling = value;
        settings.BackfaceCulling.Should().Be(value);

        settings.EnableTransparency = value;
        settings.EnableTransparency.Should().Be(value);
    }
}
