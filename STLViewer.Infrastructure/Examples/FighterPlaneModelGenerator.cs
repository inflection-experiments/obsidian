using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.Enums;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;
using STLViewer.Core.Interfaces;

namespace STLViewer.Infrastructure.Examples;

/// <summary>
/// Generates a procedural fighter plane STL model using basic geometric shapes.
/// </summary>
public class FighterPlaneModelGenerator : IPreloadedModelGenerator
{
    private readonly List<Triangle> _triangles = new();

    /// <summary>
    /// Gets the unique identifier for this model generator.
    /// </summary>
    public string ModelId => "fighter-plane";

    /// <summary>
    /// Gets the display name for this model.
    /// </summary>
    public string DisplayName => "Fighter Plane";

    /// <summary>
    /// Gets the description for this model.
    /// </summary>
    public string Description => "A detailed fighter jet model with wings, fuselage, cockpit, and engines. Perfect for demonstrating 3D visualization capabilities.";

    /// <summary>
    /// Gets the category for this model.
    /// </summary>
    public string Category => "Aircraft";

    /// <summary>
    /// Gets the estimated triangle count for this model.
    /// </summary>
    public int EstimatedTriangleCount => 200;

    /// <summary>
    /// Gets the estimated file size in bytes for this model.
    /// </summary>
    public long EstimatedSizeBytes => 10000;

    /// <summary>
    /// Gets the tags associated with this model.
    /// </summary>
    public IEnumerable<string> Tags => new[] { "aircraft", "military", "jet", "demonstration", "complex" };

    /// <summary>
    /// Generates the STL model.
    /// </summary>
    /// <returns>A result containing the generated STL model.</returns>
    public Result<STLModel> GenerateModel()
    {
        return GenerateFighterPlane();
    }

    /// <summary>
    /// Generates a fighter plane STL model.
    /// </summary>
    /// <returns>A result containing the generated STL model.</returns>
    private Result<STLModel> GenerateFighterPlane()
    {
        try
        {
            _triangles.Clear();

            // Generate the main components of the fighter plane
            GenerateFuselage();
            GenerateWings();
            GenerateTail();
            GenerateCockpit();
            GenerateEngines();

            if (_triangles.Count == 0)
                return Result<STLModel>.Fail("Failed to generate fighter plane triangles");

            // Create metadata
            var metadata = ModelMetadata.CreateDetailed(
                fileName: "FighterPlane.stl",
                fileSizeBytes: EstimateFileSize(),
                format: STLFormat.Binary,
                triangleCount: _triangles.Count,
                surfaceArea: CalculateTotalSurfaceArea(),
                boundingBox: BoundingBox.FromPoints(_triangles.SelectMany(t => t.Vertices))
            );

            // Create raw data placeholder (would be actual STL binary data in production)
            var rawData = GenerateRawSTLData();

            return STLModel.Create(metadata, _triangles, rawData);
        }
        catch (Exception ex)
        {
            return Result<STLModel>.Fail($"Error generating fighter plane model: {ex.Message}");
        }
    }

    private void GenerateFuselage()
    {
        // Main body cylinder with tapered nose
        const float radius = 0.8f;
        const int segments = 16;

        // Generate cylindrical fuselage
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(2 * System.Math.PI * i / segments);
            float angle2 = (float)(2 * System.Math.PI * (i + 1) / segments);

            // Body section (middle)
            var p1 = new Vector3(MathF.Cos(angle1) * radius, MathF.Sin(angle1) * radius, 2.0f);
            var p2 = new Vector3(MathF.Cos(angle2) * radius, MathF.Sin(angle2) * radius, 2.0f);
            var p3 = new Vector3(MathF.Cos(angle1) * radius, MathF.Sin(angle1) * radius, -2.0f);
            var p4 = new Vector3(MathF.Cos(angle2) * radius, MathF.Sin(angle2) * radius, -2.0f);

            // Side panels
            AddQuad(p1, p2, p4, p3);

            // Tapered nose
            var nosePoint = new Vector3(0, 0, 4.0f);
            AddTriangle(p1, p2, nosePoint);

            // Tapered tail
            var tailPoint = new Vector3(0, 0, -6.0f);
            AddTriangle(p3, tailPoint, p4);
        }
    }

    private void GenerateWings()
    {
        // Main wings
        var wingRoot1 = new Vector3(-0.8f, 0, 0);
        var wingRoot2 = new Vector3(0.8f, 0, 0);
        var wingTip1 = new Vector3(-4.0f, 0, -1.0f);
        var wingTip2 = new Vector3(4.0f, 0, -1.0f);

        // Wing leading edge
        var wingFront1 = new Vector3(-3.5f, 0, 1.5f);
        var wingFront2 = new Vector3(3.5f, 0, 1.5f);

        // Left wing
        AddQuad(
            wingRoot1, wingFront1, wingTip1, new Vector3(-2.0f, 0, -0.5f)
        );

        // Right wing
        AddQuad(
            wingRoot2, new Vector3(2.0f, 0, -0.5f), wingTip2, wingFront2
        );

        // Wing thickness (top surfaces)
        var wingThickness = 0.2f;
        AddQuad(
            wingRoot1 + new Vector3(0, wingThickness, 0),
            wingFront1 + new Vector3(0, wingThickness, 0),
            wingTip1 + new Vector3(0, wingThickness, 0),
            new Vector3(-2.0f, wingThickness, -0.5f)
        );

        AddQuad(
            wingRoot2 + new Vector3(0, wingThickness, 0),
            new Vector3(2.0f, wingThickness, -0.5f),
            wingTip2 + new Vector3(0, wingThickness, 0),
            wingFront2 + new Vector3(0, wingThickness, 0)
        );
    }

    private void GenerateTail()
    {
        // Vertical stabilizer
        var tailBase1 = new Vector3(0, 0, -4.0f);
        var tailBase2 = new Vector3(0, 0, -5.5f);
        var tailTop = new Vector3(0, 2.5f, -4.5f);

        AddTriangle(tailBase1, tailBase2, tailTop);
        AddTriangle(tailBase2, tailBase1, tailTop); // Back face

        // Horizontal stabilizers
        var hTailRoot1 = new Vector3(-0.3f, 0, -4.5f);
        var hTailRoot2 = new Vector3(0.3f, 0, -4.5f);
        var hTailTip1 = new Vector3(-1.5f, 0, -5.0f);
        var hTailTip2 = new Vector3(1.5f, 0, -5.0f);

        AddQuad(hTailRoot1, hTailRoot2, hTailTip2, hTailTip1);

        // Add thickness
        var hTailThickness = 0.1f;
        AddQuad(
            hTailRoot1 + new Vector3(0, hTailThickness, 0),
            hTailTip1 + new Vector3(0, hTailThickness, 0),
            hTailTip2 + new Vector3(0, hTailThickness, 0),
            hTailRoot2 + new Vector3(0, hTailThickness, 0)
        );
    }

    private void GenerateCockpit()
    {
        // Raised cockpit canopy
        const int segments = 8;
        const float canopyRadius = 0.6f;
        const float canopyHeight = 0.4f;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(System.Math.PI * i / segments);
            float angle2 = (float)(System.Math.PI * (i + 1) / segments);

            var p1 = new Vector3(
                MathF.Cos(angle1) * canopyRadius,
                MathF.Sin(angle1) * canopyRadius + canopyHeight,
                1.5f
            );

            var p2 = new Vector3(
                MathF.Cos(angle2) * canopyRadius,
                MathF.Sin(angle2) * canopyRadius + canopyHeight,
                1.5f
            );

            var p3 = new Vector3(
                MathF.Cos(angle1) * canopyRadius,
                MathF.Sin(angle1) * canopyRadius + canopyHeight,
                0.5f
            );

            var p4 = new Vector3(
                MathF.Cos(angle2) * canopyRadius,
                MathF.Sin(angle2) * canopyRadius + canopyHeight,
                0.5f
            );

            AddQuad(p1, p2, p4, p3);
        }
    }

    private void GenerateEngines()
    {
        // Engine intakes
        GenerateEngineIntake(new Vector3(-0.5f, -0.3f, -1.0f));
        GenerateEngineIntake(new Vector3(0.5f, -0.3f, -1.0f));

        // Engine nozzles
        GenerateEngineNozzle(new Vector3(-0.4f, -0.2f, -5.8f));
        GenerateEngineNozzle(new Vector3(0.4f, -0.2f, -5.8f));
    }

    private void GenerateEngineIntake(Vector3 center)
    {
        const int segments = 8;
        const float radius = 0.25f;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(2 * System.Math.PI * i / segments);
            float angle2 = (float)(2 * System.Math.PI * (i + 1) / segments);

            var p1 = center + new Vector3(MathF.Cos(angle1) * radius, MathF.Sin(angle1) * radius, 0);
            var p2 = center + new Vector3(MathF.Cos(angle2) * radius, MathF.Sin(angle2) * radius, 0);
            var p3 = center + new Vector3(MathF.Cos(angle1) * radius * 0.8f, MathF.Sin(angle1) * radius * 0.8f, -0.5f);

            AddTriangle(p1, p2, p3);
        }
    }

    private void GenerateEngineNozzle(Vector3 center)
    {
        const int segments = 6;
        const float radius = 0.2f;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(2 * System.Math.PI * i / segments);
            float angle2 = (float)(2 * System.Math.PI * (i + 1) / segments);

            var p1 = center + new Vector3(MathF.Cos(angle1) * radius, MathF.Sin(angle1) * radius, 0);
            var p2 = center + new Vector3(MathF.Cos(angle2) * radius, MathF.Sin(angle2) * radius, 0);
            var p3 = center + new Vector3(MathF.Cos(angle1) * radius * 1.2f, MathF.Sin(angle1) * radius * 1.2f, -0.3f);

            AddTriangle(p1, p3, p2);
        }
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var normal = Triangle.CalculateNormal(v1, v2, v3);
        var triangle = new Triangle(v1, v2, v3, normal);

        if (triangle.IsValid)
        {
            _triangles.Add(triangle);
        }
    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        // Split quad into two triangles
        AddTriangle(v1, v2, v3);
        AddTriangle(v1, v3, v4);
    }

    private float CalculateTotalSurfaceArea()
    {
        return _triangles.Sum(t => t.Area);
    }

    private long EstimateFileSize()
    {
        // STL binary format: 80 byte header + 4 byte count + (50 bytes per triangle)
        return 80 + 4 + (_triangles.Count * 50);
    }

    private byte[] GenerateRawSTLData()
    {
        // Generate minimal STL binary data for the metadata
        // In a real implementation, this would be the actual STL file bytes
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        // Header (80 bytes)
        var header = "Fighter Plane Model - Generated".PadRight(80, '\0');
        writer.Write(System.Text.Encoding.ASCII.GetBytes(header.Substring(0, 80)));

        // Triangle count
        writer.Write((uint)_triangles.Count);

        // Triangles
        foreach (var triangle in _triangles)
        {
            // Normal
            writer.Write(triangle.Normal.X);
            writer.Write(triangle.Normal.Y);
            writer.Write(triangle.Normal.Z);

            // Vertices
            writer.Write(triangle.Vertex1.X);
            writer.Write(triangle.Vertex1.Y);
            writer.Write(triangle.Vertex1.Z);

            writer.Write(triangle.Vertex2.X);
            writer.Write(triangle.Vertex2.Y);
            writer.Write(triangle.Vertex2.Z);

            writer.Write(triangle.Vertex3.X);
            writer.Write(triangle.Vertex3.Y);
            writer.Write(triangle.Vertex3.Z);

            // Attribute byte count
            writer.Write((ushort)0);
        }

        return stream.ToArray();
    }
}
