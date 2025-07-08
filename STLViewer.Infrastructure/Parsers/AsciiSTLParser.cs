using System.Globalization;
using System.Text;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Infrastructure.Parsers;

/// <summary>
/// Parser for ASCII STL files.
/// </summary>
public class AsciiSTLParser
{
    private const int MaxLineLength = 1024;
    private const int MaxTriangles = 10_000_000; // Safety limit

    /// <summary>
    /// Parses an ASCII STL file from byte data.
    /// </summary>
    /// <param name="data">The STL file data.</param>
    /// <param name="fileName">The original filename.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the parsed STL model or error information.</returns>
    public async Task<Result<STLModel>> ParseAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var triangles = new List<Triangle>();
            var lineNumber = 0;
            string? solidName = null;

            await foreach (var line in ReadLinesAsync(reader, cancellationToken))
            {
                lineNumber++;
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                var command = parts[0].ToLowerInvariant();

                switch (command)
                {
                    case "solid":
                        solidName = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "unnamed";
                        break;

                    case "facet":
                        var triangleResult = await ParseTriangleAsync(reader, lineNumber, cancellationToken);
                        if (triangleResult.IsFailure)
                            return Result<STLModel>.Fail($"Error parsing triangle at line {lineNumber}: {triangleResult.Error}");

                        triangles.Add(triangleResult.Value);

                        if (triangles.Count > MaxTriangles)
                            return Result<STLModel>.Fail($"File contains too many triangles (max {MaxTriangles:N0})");

                        break;

                    case "endsolid":
                        // End of solid - could have multiple solids in one file
                        break;

                    default:
                        // Ignore unknown commands
                        break;
                }
            }

            if (triangles.Count == 0)
                return Result<STLModel>.Fail("No triangles found in STL file");

            // Create STL model
            var modelResult = STLModel.CreateFromTriangles(
                fileName,
                triangles,
                data,
                Domain.Enums.STLFormat.ASCII
            );

            return modelResult;
        }
        catch (Exception ex)
        {
            return Result<STLModel>.Fail($"Error parsing ASCII STL file: {ex.Message}");
        }
    }

    private async Task<Result<Triangle>> ParseTriangleAsync(StreamReader reader, int startLineNumber, CancellationToken cancellationToken)
    {
        try
        {
            Vector3 normal = Vector3.Zero;
            var vertices = new List<Vector3>();
            var expectingVertices = false;

            await foreach (var line in ReadLinesAsync(reader, cancellationToken))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                var parts = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                var command = parts[0].ToLowerInvariant();

                switch (command)
                {
                    case "facet":
                        if (parts.Length >= 5 && parts[1].ToLowerInvariant() == "normal")
                        {
                            if (!TryParseVector3(parts, 2, out normal))
                                return Result<Triangle>.Fail("Invalid normal vector format");
                        }
                        break;

                    case "outer":
                        if (parts.Length >= 2 && parts[1].ToLowerInvariant() == "loop")
                        {
                            expectingVertices = true;
                        }
                        break;

                    case "vertex":
                        if (!expectingVertices)
                            return Result<Triangle>.Fail("Vertex found outside of loop");

                        if (parts.Length < 4)
                            return Result<Triangle>.Fail("Invalid vertex format - expected 3 coordinates");

                        if (!TryParseVector3(parts, 1, out var vertex))
                            return Result<Triangle>.Fail("Invalid vertex coordinates");

                        vertices.Add(vertex);

                        if (vertices.Count > 3)
                            return Result<Triangle>.Fail("Too many vertices in triangle");

                        break;

                    case "endloop":
                        expectingVertices = false;
                        break;

                    case "endfacet":
                        if (vertices.Count != 3)
                            return Result<Triangle>.Fail($"Triangle must have exactly 3 vertices, found {vertices.Count}");

                        // If normal is zero, calculate it from vertices
                        if (normal.Length < float.Epsilon)
                        {
                            normal = Triangle.CalculateNormal(vertices[0], vertices[1], vertices[2]);
                        }

                        var triangle = new Triangle(vertices[0], vertices[1], vertices[2], normal);

                        if (!triangle.IsValid)
                            return Result<Triangle>.Fail("Generated triangle is invalid");

                        return Result<Triangle>.Ok(triangle);

                    default:
                        // Ignore unknown commands
                        break;
                }
            }

            return Result<Triangle>.Fail("Unexpected end of file while parsing triangle");
        }
        catch (Exception ex)
        {
            return Result<Triangle>.Fail($"Error parsing triangle: {ex.Message}");
        }
    }

    private static bool TryParseVector3(string[] parts, int startIndex, out Vector3 vector)
    {
        vector = Vector3.Zero;

        if (parts.Length < startIndex + 3)
            return false;

        if (!float.TryParse(parts[startIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !float.TryParse(parts[startIndex + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !float.TryParse(parts[startIndex + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            return false;
        }

        // Check for valid floating point values
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z))
            return false;

        vector = new Vector3(x, y, z);
        return true;
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(StreamReader reader, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (line.Length > MaxLineLength)
                throw new InvalidOperationException($"Line too long (max {MaxLineLength} characters)");

            yield return line;
        }
    }

    /// <summary>
    /// Saves an STL model to a file in ASCII format.
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
            return Result.Fail($"Error saving ASCII STL file: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves an STL model to a stream in ASCII format.
    /// </summary>
    /// <param name="model">The STL model to save.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public async Task<Result> SaveAsync(STLModel model, Stream stream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true);

            // Write header
            var solidName = string.IsNullOrEmpty(model.Metadata.FileName) ? "stl_model" : model.Metadata.FileName;
            await writer.WriteLineAsync($"solid {solidName}");

            // Write triangles
            foreach (var triangle in model.Triangles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await WriteTriangleAsync(writer, triangle, cancellationToken);
            }

            // Write footer
            await writer.WriteLineAsync($"endsolid {solidName}");
            await writer.FlushAsync();

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("Save operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error saving ASCII STL to stream: {ex.Message}");
        }
    }

    private async Task WriteTriangleAsync(StreamWriter writer, Triangle triangle, CancellationToken cancellationToken)
    {
        var normal = triangle.Normal;
        var v1 = triangle.Vertex1;
        var v2 = triangle.Vertex2;
        var v3 = triangle.Vertex3;

        await writer.WriteLineAsync($"  facet normal {normal.X:E} {normal.Y:E} {normal.Z:E}");
        await writer.WriteLineAsync("    outer loop");
        await writer.WriteLineAsync($"      vertex {v1.X:E} {v1.Y:E} {v1.Z:E}");
        await writer.WriteLineAsync($"      vertex {v2.X:E} {v2.Y:E} {v2.Z:E}");
        await writer.WriteLineAsync($"      vertex {v3.X:E} {v3.Y:E} {v3.Z:E}");
        await writer.WriteLineAsync("    endloop");
        await writer.WriteLineAsync("  endfacet");
    }
}
