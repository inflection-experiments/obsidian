using FluentAssertions;
using STLViewer.Domain.Enums;
using STLViewer.Infrastructure.Examples;
using STLViewer.Infrastructure.Parsers;
using Xunit;

namespace STLViewer.Infrastructure.Tests.Parsers;

public class STLParserServiceTests
{
    private readonly STLParserService _parser;

    public STLParserServiceTests()
    {
        _parser = new STLParserService();
    }

    [Fact]
    public async Task ParseAsync_SimpleAsciiSTL_ShouldSucceed()
    {
        // Arrange
        var data = SampleSTLData.SimpleAsciiSTLBytes;
        var fileName = "simple.stl";

        // Act
        var result = await _parser.ParseAsync(data, fileName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TriangleCount.Should().Be(1);
        result.Value.Metadata.Format.Should().Be(STLFormat.ASCII);
        result.Value.Metadata.FileName.Should().Be(fileName);
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsync_SimpleBinarySTL_ShouldSucceed()
    {
        // Arrange
        var data = SampleSTLData.SimpleBinarySTL;
        var fileName = "simple.stl";

        // Act
        var result = await _parser.ParseAsync(data, fileName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TriangleCount.Should().Be(1);
        result.Value.Metadata.Format.Should().Be(STLFormat.Binary);
        result.Value.Metadata.FileName.Should().Be(fileName);
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ParseAsync_CubeAsciiSTL_ShouldSucceed()
    {
        // Arrange
        var data = SampleSTLData.CubeAsciiSTLBytes;
        var fileName = "cube.stl";

        // Act
        var result = await _parser.ParseAsync(data, fileName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TriangleCount.Should().Be(12); // Cube has 12 triangles (6 faces * 2 triangles)
        result.Value.Metadata.Format.Should().Be(STLFormat.ASCII);
        result.Value.Metadata.FileName.Should().Be(fileName);
        result.Value.IsValid.Should().BeTrue();

        // Verify bounding box
        var boundingBox = result.Value.BoundingBox;
        boundingBox.Min.X.Should().BeApproximately(0.0f, 0.001f);
        boundingBox.Min.Y.Should().BeApproximately(0.0f, 0.001f);
        boundingBox.Min.Z.Should().BeApproximately(0.0f, 0.001f);
        boundingBox.Max.X.Should().BeApproximately(1.0f, 0.001f);
        boundingBox.Max.Y.Should().BeApproximately(1.0f, 0.001f);
        boundingBox.Max.Z.Should().BeApproximately(1.0f, 0.001f);
    }

    [Fact]
    public void DetectFormat_AsciiSTL_ShouldReturnAscii()
    {
        // Arrange
        var data = SampleSTLData.SimpleAsciiSTLBytes;

        // Act
        var format = _parser.DetectFormat(data);

        // Assert
        format.Should().Be(STLFormat.ASCII);
    }

    [Fact]
    public void DetectFormat_BinarySTL_ShouldReturnBinary()
    {
        // Arrange
        var data = SampleSTLData.SimpleBinarySTL;

        // Act
        var format = _parser.DetectFormat(data);

        // Assert
        format.Should().Be(STLFormat.Binary);
    }

    [Fact]
    public async Task ParseAsync_EmptyData_ShouldFail()
    {
        // Arrange
        var data = Array.Empty<byte>();
        var fileName = "empty.stl";

        // Act
        var result = await _parser.ParseAsync(data, fileName);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No data provided");
    }

    [Fact]
    public async Task ParseAsync_InvalidData_ShouldFail()
    {
        // Arrange
        var data = "This is not STL data"u8.ToArray();
        var fileName = "invalid.stl";

        // Act
        var result = await _parser.ParseAsync(data, fileName);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DetectFormat_EmptyData_ShouldReturnUnknown()
    {
        // Arrange
        var data = Array.Empty<byte>();

        // Act
        var format = _parser.DetectFormat(data);

        // Assert
        format.Should().Be(STLFormat.Unknown);
    }
}
