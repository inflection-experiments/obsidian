using System.Text;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Infrastructure.Parsers;

/// <summary>
/// Parser for binary STL files.
/// </summary>
public class BinarySTLParser
{
    private const int HeaderSize = 80;
    private const int TriangleSize = 50; // 12 (normal) + 36 (vertices) + 2 (attribute)
    private const int MaxTriangles = 10_000_000; // Safety limit

    /// <summary>
    /// Parses a binary STL file from byte data.
    /// </summary>
    /// <param name="data">The STL file data.</param>
    /// <param name="fileName">The original filename.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the parsed STL model or error information.</returns>
    public async Task<Result<STLModel>> ParseAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Minimum file size check
            if (data.Length < HeaderSize + sizeof(uint))
                return Result<STLModel>.Fail("File too small to be a valid binary STL file");

            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            // Read header (80 bytes)
            var header = reader.ReadBytes(HeaderSize);
            var headerText = Encoding.UTF8.GetString(header).TrimEnd('\0');

            // Read triangle count
            var triangleCount = reader.ReadUInt32();

            if (triangleCount > MaxTriangles)
                return Result<STLModel>.Fail($"File contains too many triangles: {triangleCount:N0} (max {MaxTriangles:N0})");

            // Validate expected file size
            var expectedSize = HeaderSize + sizeof(uint) + (triangleCount * TriangleSize);
            if (data.Length < expectedSize)
                return Result<STLModel>.Fail($"File size mismatch. Expected at least {expectedSize} bytes, got {data.Length} bytes");

            var triangles = new List<Triangle>((int)triangleCount);

            // Read triangles
            for (uint i = 0; i < triangleCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var triangleResult = await ParseTriangleAsync(reader, i, cancellationToken);
                if (triangleResult.IsFailure)
                    return Result<STLModel>.Fail($"Error parsing triangle {i + 1}: {triangleResult.Error}");

                triangles.Add(triangleResult.Value);
            }

            if (triangles.Count == 0)
                return Result<STLModel>.Fail("No triangles found in STL file");

            // Create additional metadata from header if available
            var additionalProperties = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(headerText))
            {
                additionalProperties["Header"] = headerText;
            }

            // Create STL model
            var modelResult = STLModel.CreateFromTriangles(
                fileName,
                triangles,
                data,
                Domain.Enums.STLFormat.Binary
            );

            return modelResult;
        }
        catch (EndOfStreamException)
        {
            return Result<STLModel>.Fail("Unexpected end of file while reading binary STL data");
        }
        catch (Exception ex)
        {
            return Result<STLModel>.Fail($"Error parsing binary STL file: {ex.Message}");
        }
    }

    private static async Task<Result<Triangle>> ParseTriangleAsync(BinaryReader reader, uint triangleIndex, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read normal vector (12 bytes)
            var normalX = reader.ReadSingle();
            var normalY = reader.ReadSingle();
            var normalZ = reader.ReadSingle();

            // Validate normal vector
            if (!IsValidFloat(normalX) || !IsValidFloat(normalY) || !IsValidFloat(normalZ))
                return Result<Triangle>.Fail($"Invalid normal vector at triangle {triangleIndex + 1}");

            var normal = new Vector3(normalX, normalY, normalZ);

            // Read vertices (36 bytes)
            var vertices = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                var x = reader.ReadSingle();
                var y = reader.ReadSingle();
                var z = reader.ReadSingle();

                // Validate vertex coordinates
                if (!IsValidFloat(x) || !IsValidFloat(y) || !IsValidFloat(z))
                    return Result<Triangle>.Fail($"Invalid vertex {i + 1} coordinates at triangle {triangleIndex + 1}");

                vertices[i] = new Vector3(x, y, z);
            }

            // Read attribute byte count (2 bytes) - usually 0
            var attributeByteCount = reader.ReadUInt16();

            // Skip attribute bytes if present
            if (attributeByteCount > 0)
            {
                var attributeBytes = reader.ReadBytes(attributeByteCount);
                // Could potentially extract color information here if needed
            }

            // If normal is zero or very small, calculate it from vertices
            if (normal.Length < float.Epsilon)
            {
                normal = Triangle.CalculateNormal(vertices[0], vertices[1], vertices[2]);
            }

            var triangle = new Triangle(vertices[0], vertices[1], vertices[2], normal);

            if (!triangle.IsValid)
                return Result<Triangle>.Fail($"Generated triangle {triangleIndex + 1} is invalid");

            return await Task.FromResult(Result<Triangle>.Ok(triangle));
        }
        catch (EndOfStreamException)
        {
            return Result<Triangle>.Fail($"Unexpected end of file while reading triangle {triangleIndex + 1}");
        }
        catch (Exception ex)
        {
            return Result<Triangle>.Fail($"Error parsing triangle {triangleIndex + 1}: {ex.Message}");
        }
    }

    private static bool IsValidFloat(float value)
    {
        return float.IsFinite(value) && !float.IsNaN(value);
    }

    /// <summary>
    /// Validates if the data appears to be a valid binary STL file.
    /// </summary>
    /// <param name="data">The file data to validate.</param>
    /// <returns>True if the data appears to be a valid binary STL file.</returns>
    public static bool IsValidBinarySTL(byte[] data)
    {
        if (data.Length < HeaderSize + sizeof(uint))
            return false;

        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            // Skip header
            reader.ReadBytes(HeaderSize);

            // Read triangle count
            var triangleCount = reader.ReadUInt32();

            // Check if triangle count is reasonable
            if (triangleCount == 0 || triangleCount > MaxTriangles)
                return false;

            // Check if file size matches expected size
            var expectedSize = HeaderSize + sizeof(uint) + (triangleCount * TriangleSize);
            return data.Length >= expectedSize;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the header text from a binary STL file.
    /// </summary>
    /// <param name="data">The STL file data.</param>
    /// <returns>The header text, or null if the file is invalid.</returns>
    public static string? ExtractHeader(byte[] data)
    {
        if (data.Length < HeaderSize)
            return null;

        try
        {
            var headerBytes = data.Take(HeaderSize).ToArray();
            return Encoding.UTF8.GetString(headerBytes).TrimEnd('\0').Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the triangle count from a binary STL file without parsing the entire file.
    /// </summary>
    /// <param name="data">The STL file data.</param>
    /// <returns>The triangle count, or null if the file is invalid.</returns>
    public static uint? GetTriangleCount(byte[] data)
    {
        if (data.Length < HeaderSize + sizeof(uint))
            return null;

        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            // Skip header
            reader.ReadBytes(HeaderSize);

            // Read triangle count
            return reader.ReadUInt32();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Saves an STL model to a file in binary format.
    /// </summary>
    /// <param name="model">The STL model to save.</param>
    /// <param name="filePath">The path where to save the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SaveAsync(STLModel model, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            return await SaveAsync(model, fileStream, cancellationToken);
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error saving binary STL file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves an STL model to a stream in binary format.
    /// </summary>
    /// <param name="model">The STL model to save.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SaveAsync(STLModel model, Stream stream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

            // Write header (80 bytes)
            var headerText = string.IsNullOrEmpty(model.Metadata.FileName) ? "Binary STL" : model.Metadata.FileName;
            var headerBytes = new byte[HeaderSize];
            var textBytes = Encoding.UTF8.GetBytes(headerText);
            Array.Copy(textBytes, 0, headerBytes, 0, System.Math.Min(textBytes.Length, HeaderSize - 1));
            writer.Write(headerBytes);

            // Write triangle count
            var triangleCount = (uint)model.Triangles.Count;
            writer.Write(triangleCount);

            // Write triangles
            foreach (var triangle in model.Triangles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await WriteTriangleAsync(writer, triangle, cancellationToken);
            }

            writer.Flush();
            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Save operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error saving binary STL to stream: {ex.Message}");
        }
    }

    private async Task WriteTriangleAsync(BinaryWriter writer, Triangle triangle, CancellationToken cancellationToken)
    {
        // Write normal vector (12 bytes)
        writer.Write(triangle.Normal.X);
        writer.Write(triangle.Normal.Y);
        writer.Write(triangle.Normal.Z);

        // Write vertices (36 bytes)
        writer.Write(triangle.Vertex1.X);
        writer.Write(triangle.Vertex1.Y);
        writer.Write(triangle.Vertex1.Z);

        writer.Write(triangle.Vertex2.X);
        writer.Write(triangle.Vertex2.Y);
        writer.Write(triangle.Vertex2.Z);

        writer.Write(triangle.Vertex3.X);
        writer.Write(triangle.Vertex3.Y);
        writer.Write(triangle.Vertex3.Z);

        // Write attribute byte count (2 bytes) - typically 0
        writer.Write((ushort)0);

        await Task.CompletedTask; // For async pattern consistency
    }
}
