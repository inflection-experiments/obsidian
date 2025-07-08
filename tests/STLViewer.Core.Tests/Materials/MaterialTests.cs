using FluentAssertions;
using STLViewer.Core.Interfaces;
using STLViewer.Math;
using Xunit;

namespace STLViewer.Core.Tests.Materials;

/// <summary>
/// Tests for the Material class and related functionality.
/// </summary>
public class MaterialTests
{
    [Fact]
    public void Material_DefaultConstructor_ShouldHaveExpectedDefaults()
    {
        // Arrange & Act
        var material = new Material();

        // Assert
        material.DiffuseColor.Should().Be(Color.LightGray);
        material.AmbientColor.Should().Be(new Color(0.2f, 0.2f, 0.2f));
        material.SpecularColor.Should().Be(Color.White);
        material.EmissiveColor.Should().Be(Color.Black);
        material.Shininess.Should().Be(32.0f);
        material.Alpha.Should().Be(1.0f);
        material.Metallic.Should().Be(0.0f);
        material.Roughness.Should().Be(0.5f);
        material.Preset.Should().Be(MaterialPreset.Default);
    }

    [Theory]
    [InlineData(MaterialPreset.Metal)]
    [InlineData(MaterialPreset.Plastic)]
    [InlineData(MaterialPreset.Matte)]
    [InlineData(MaterialPreset.Glossy)]
    [InlineData(MaterialPreset.Custom)]
    public void Material_FromPreset_ShouldCreateMaterialWithCorrectPreset(MaterialPreset preset)
    {
        // Arrange & Act
        var material = Material.FromPreset(preset);

        // Assert
        material.Preset.Should().Be(preset);
        material.Should().NotBeNull();
    }

    [Fact]
    public void Material_FromPreset_Metal_ShouldHaveMetallicProperties()
    {
        // Arrange & Act
        var material = Material.FromPreset(MaterialPreset.Metal);

        // Assert
        material.Metallic.Should().Be(0.9f);
        material.Roughness.Should().Be(0.1f);
        material.Shininess.Should().Be(128.0f);
        material.SpecularColor.Should().Be(Color.White);
        material.Alpha.Should().Be(1.0f);
    }

    [Fact]
    public void Material_FromPreset_Plastic_ShouldHavePlasticProperties()
    {
        // Arrange & Act
        var material = Material.FromPreset(MaterialPreset.Plastic);

        // Assert
        material.Metallic.Should().Be(0.0f);
        material.Roughness.Should().Be(0.3f);
        material.Shininess.Should().Be(64.0f);
        material.SpecularColor.Should().Be(new Color(0.5f, 0.5f, 0.5f, 1.0f));
        material.Alpha.Should().Be(1.0f);
    }

    [Fact]
    public void Material_FromPreset_Matte_ShouldHaveMatteProperties()
    {
        // Arrange & Act
        var material = Material.FromPreset(MaterialPreset.Matte);

        // Assert
        material.Metallic.Should().Be(0.0f);
        material.Roughness.Should().Be(1.0f);
        material.Shininess.Should().Be(1.0f);
        material.SpecularColor.Should().Be(Color.Black);
        material.Alpha.Should().Be(1.0f);
    }

    [Fact]
    public void Material_FromPreset_Glossy_ShouldHaveGlossyProperties()
    {
        // Arrange & Act
        var material = Material.FromPreset(MaterialPreset.Glossy);

        // Assert
        material.Metallic.Should().Be(0.2f);
        material.Roughness.Should().Be(0.05f);
        material.Shininess.Should().Be(256.0f);
        material.SpecularColor.Should().Be(Color.White);
        material.Alpha.Should().Be(1.0f);
    }

    [Fact]
    public void Material_FromPreset_WithCustomColor_ShouldUseProvidedColor()
    {
        // Arrange
        var customColor = new Color(1.0f, 0.0f, 0.0f, 1.0f); // Red

        // Act
        var material = Material.FromPreset(MaterialPreset.Metal, customColor);

        // Assert
        material.DiffuseColor.Should().Be(customColor);
        material.Preset.Should().Be(MaterialPreset.Metal);
    }

    [Fact]
    public void Material_FromPreset_WithCustomColor_ShouldUpdateAmbientColor()
    {
        // Arrange
        var customColor = new Color(1.0f, 0.5f, 0.0f, 1.0f); // Orange

        // Act
        var material = Material.FromPreset(MaterialPreset.Metal, customColor);

        // Assert
        material.AmbientColor.R.Should().BeApproximately(0.1f, 0.001f); // 10% of diffuse
        material.AmbientColor.G.Should().BeApproximately(0.05f, 0.001f); // 10% of diffuse
        material.AmbientColor.B.Should().BeApproximately(0.0f, 0.001f); // 10% of diffuse
    }

    [Fact]
    public void Material_Clone_ShouldCreateExactCopy()
    {
        // Arrange
        var original = Material.FromPreset(MaterialPreset.Metal, new Color(0.8f, 0.2f, 0.1f));
        original.Alpha = 0.7f;
        original.Shininess = 99.0f;

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.DiffuseColor.Should().Be(original.DiffuseColor);
        clone.AmbientColor.Should().Be(original.AmbientColor);
        clone.SpecularColor.Should().Be(original.SpecularColor);
        clone.EmissiveColor.Should().Be(original.EmissiveColor);
        clone.Shininess.Should().Be(original.Shininess);
        clone.Alpha.Should().Be(original.Alpha);
        clone.Metallic.Should().Be(original.Metallic);
        clone.Roughness.Should().Be(original.Roughness);
        clone.Preset.Should().Be(original.Preset);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Material_Alpha_ShouldAcceptValidRange(float alpha)
    {
        // Arrange
        var material = new Material();

        // Act
        material.Alpha = alpha;

        // Assert
        material.Alpha.Should().Be(alpha);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Material_Metallic_ShouldAcceptValidRange(float metallic)
    {
        // Arrange
        var material = new Material();

        // Act
        material.Metallic = metallic;

        // Assert
        material.Metallic.Should().Be(metallic);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Material_Roughness_ShouldAcceptValidRange(float roughness)
    {
        // Arrange
        var material = new Material();

        // Act
        material.Roughness = roughness;

        // Assert
        material.Roughness.Should().Be(roughness);
    }

    [Theory]
    [InlineData(1.0f)]
    [InlineData(32.0f)]
    [InlineData(256.0f)]
    public void Material_Shininess_ShouldAcceptPositiveValues(float shininess)
    {
        // Arrange
        var material = new Material();

        // Act
        material.Shininess = shininess;

        // Assert
        material.Shininess.Should().Be(shininess);
    }
}
