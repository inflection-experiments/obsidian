using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Domain.Entities;

/// <summary>
/// Represents an STL 3D model with triangular mesh data.
/// This is the aggregate root for the STL model domain.
/// </summary>
public sealed class STLModel : Entity<Guid>
{
    private readonly List<Triangle> _triangles;

    /// <summary>
    /// Gets the metadata information about this STL model.
    /// </summary>
    public ModelMetadata Metadata { get; private set; }

    /// <summary>
    /// Gets the raw binary data of the STL file.
    /// </summary>
    public byte[] RawData { get; private set; }

    /// <summary>
    /// Gets the collection of triangles that make up this model.
    /// </summary>
    public IReadOnlyList<Triangle> Triangles => _triangles.AsReadOnly();

    /// <summary>
    /// Gets the bounding box that encompasses the entire model.
    /// </summary>
    public BoundingBox BoundingBox => Metadata.BoundingBox;

    /// <summary>
    /// Gets the number of triangles in this model.
    /// </summary>
    public int TriangleCount => _triangles.Count;

    /// <summary>
    /// Gets the total surface area of the model.
    /// </summary>
    public float SurfaceArea => _triangles.Sum(t => t.Area);

    /// <summary>
    /// Gets a value indicating whether this model appears to be a valid 3D mesh.
    /// </summary>
    public bool IsValid => _triangles.All(t => t.IsValid) && TriangleCount > 0;

    /// <summary>
    /// Gets the center point of the model.
    /// </summary>
    public Vector3 Center => BoundingBox.Center;

    /// <summary>
    /// Gets the dimensions of the model.
    /// </summary>
    public Vector3 Dimensions => BoundingBox.Size;

    // Private constructor for entity framework
    private STLModel()
    {
        _triangles = new List<Triangle>();
        Metadata = null!;
        RawData = Array.Empty<byte>();
    }

    /// <summary>
    /// Initializes a new instance of the STLModel class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="metadata">The model metadata.</param>
    /// <param name="triangles">The collection of triangles.</param>
    /// <param name="rawData">The raw STL file data.</param>
    private STLModel(Guid id, ModelMetadata metadata, IEnumerable<Triangle> triangles, byte[] rawData)
    {
        Id = id;
        Metadata = metadata;
        _triangles = triangles.ToList();
        RawData = rawData;
    }

    /// <summary>
    /// Creates a new STL model from parsed triangle data.
    /// </summary>
    /// <param name="metadata">The model metadata.</param>
    /// <param name="triangles">The collection of triangles.</param>
    /// <param name="rawData">The raw STL file data.</param>
    /// <returns>A result containing the created STL model or error information.</returns>
    public static Result<STLModel> Create(
        ModelMetadata metadata,
        IEnumerable<Triangle> triangles,
        byte[] rawData)
    {
        // Validation
        if (metadata == null)
            return Result<STLModel>.Fail("Metadata cannot be null");

        if (rawData == null || rawData.Length == 0)
            return Result<STLModel>.Fail("Raw data cannot be null or empty");

        var triangleList = triangles?.ToList() ?? new List<Triangle>();

        if (triangleList.Count == 0)
            return Result<STLModel>.Fail("At least one triangle is required");

        // Validate triangles
        var invalidTriangles = triangleList.Where(t => !t.IsValid).ToList();
        if (invalidTriangles.Any())
        {
            return Result<STLModel>.Fail($"Model contains {invalidTriangles.Count} invalid triangles");
        }

        // Calculate bounding box if not provided
        var boundingBox = metadata.BoundingBox;
        if (boundingBox == BoundingBox.Empty)
        {
            var allVertices = triangleList.SelectMany(t => t.Vertices);
            boundingBox = BoundingBox.FromPoints(allVertices);

            // Update metadata with calculated bounding box
            metadata = metadata with { BoundingBox = boundingBox };
        }

        // Validate triangle count matches metadata
        if (metadata.TriangleCount != triangleList.Count)
        {
            metadata = metadata with { TriangleCount = triangleList.Count };
        }

        var model = new STLModel(Guid.NewGuid(), metadata, triangleList, rawData);
        return Result.Ok(model);
    }

    /// <summary>
    /// Creates an STL model from minimal information, calculating metadata automatically.
    /// </summary>
    /// <param name="fileName">The filename of the STL file.</param>
    /// <param name="triangles">The collection of triangles.</param>
    /// <param name="rawData">The raw STL file data.</param>
    /// <param name="format">The STL format.</param>
    /// <returns>A result containing the created STL model or error information.</returns>
    public static Result<STLModel> CreateFromTriangles(
        string fileName,
        IEnumerable<Triangle> triangles,
        byte[] rawData,
        Domain.Enums.STLFormat format)
    {
        var triangleList = triangles?.ToList() ?? new List<Triangle>();

        if (triangleList.Count == 0)
            return Result<STLModel>.Fail("At least one triangle is required");

        // Calculate metadata
        var allVertices = triangleList.SelectMany(t => t.Vertices);
        var boundingBox = BoundingBox.FromPoints(allVertices);
        var surfaceArea = triangleList.Sum(t => t.Area);
        var degenerateCount = triangleList.Count(t => t.IsDegenerate);

        // Calculate edge statistics
        var edges = triangleList.SelectMany(t => new[]
        {
            Vector3.Distance(t.Vertex1, t.Vertex2),
            Vector3.Distance(t.Vertex2, t.Vertex3),
            Vector3.Distance(t.Vertex3, t.Vertex1)
        }).Where(length => length > 0).ToList();

        var minEdgeLength = edges.Any() ? edges.Min() : (float?)null;
        var maxEdgeLength = edges.Any() ? edges.Max() : (float?)null;
        var averageEdgeLength = edges.Any() ? edges.Average() : (float?)null;

        var metadata = ModelMetadata.CreateDetailed(
            fileName: fileName,
            fileSizeBytes: rawData.Length,
            format: format,
            triangleCount: triangleList.Count,
            surfaceArea: surfaceArea,
            boundingBox: boundingBox,
            volume: null, // Volume calculation would require mesh analysis
            degenerateTriangleCount: degenerateCount,
            minEdgeLength: minEdgeLength,
            maxEdgeLength: maxEdgeLength,
            averageEdgeLength: averageEdgeLength
        );

        return Create(metadata, triangleList, rawData);
    }

    /// <summary>
    /// Updates the model's metadata.
    /// </summary>
    /// <param name="newMetadata">The new metadata.</param>
    public void UpdateMetadata(ModelMetadata newMetadata)
    {
        if (newMetadata == null)
            throw new ArgumentNullException(nameof(newMetadata));

        Metadata = newMetadata;
        MarkAsUpdated();
    }

    /// <summary>
    /// Gets triangles within the specified bounding box.
    /// </summary>
    /// <param name="bounds">The bounding box to search within.</param>
    /// <returns>The triangles within the specified bounds.</returns>
    public IEnumerable<Triangle> GetTrianglesInBounds(BoundingBox bounds)
    {
        return _triangles.Where(triangle =>
            bounds.Intersects(triangle.BoundingBox));
    }

    /// <summary>
    /// Gets triangles that are closest to the specified point.
    /// </summary>
    /// <param name="point">The point to search near.</param>
    /// <param name="maxCount">The maximum number of triangles to return.</param>
    /// <returns>The closest triangles ordered by distance.</returns>
    public IEnumerable<Triangle> GetClosestTriangles(Vector3 point, int maxCount = 10)
    {
        return _triangles
            .Select(triangle => new { Triangle = triangle, Distance = triangle.BoundingBox.DistanceTo(point) })
            .OrderBy(x => x.Distance)
            .Take(maxCount)
            .Select(x => x.Triangle);
    }

    /// <summary>
    /// Calculates various statistics about the model.
    /// </summary>
    /// <returns>A dictionary containing statistical information.</returns>
    public Dictionary<string, object> CalculateStatistics()
    {
        var stats = new Dictionary<string, object>
        {
            ["TriangleCount"] = TriangleCount,
            ["SurfaceArea"] = SurfaceArea,
            ["Volume"] = BoundingBox.Volume,
            ["Dimensions"] = Dimensions,
            ["Center"] = Center,
            ["BoundingBox"] = BoundingBox
        };

        if (_triangles.Any())
        {
            var areas = _triangles.Select(t => t.Area).ToList();
            stats["MinTriangleArea"] = areas.Min();
            stats["MaxTriangleArea"] = areas.Max();
            stats["AverageTriangleArea"] = areas.Average();
            stats["DegenerateTriangles"] = _triangles.Count(t => t.IsDegenerate);
        }

        return stats;
    }

    /// <summary>
    /// Transforms all triangles in the model by the specified matrix.
    /// </summary>
    /// <param name="transformMatrix">The transformation matrix.</param>
    /// <returns>A new STL model with transformed triangles.</returns>
    public STLModel Transform(Matrix4x4 transformMatrix)
    {
        var transformedTriangles = _triangles.Select(t => t.Transform(transformMatrix));
        var transformedBoundingBox = BoundingBox.Transform(transformMatrix);

        var newMetadata = Metadata with
        {
            BoundingBox = transformedBoundingBox,
            LoadedAt = DateTime.UtcNow
        };

        var result = Create(newMetadata, transformedTriangles, RawData);
        return result.IsSuccess ? result.Value : throw new InvalidOperationException("Failed to create transformed model");
    }

    /// <summary>
    /// Creates a copy of this model with only triangles that pass the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter triangles.</param>
    /// <returns>A new STL model containing only the filtered triangles.</returns>
    public Result<STLModel> Filter(Func<Triangle, bool> predicate)
    {
        if (predicate == null)
            return Result<STLModel>.Fail("Predicate cannot be null");

        var filteredTriangles = _triangles.Where(predicate).ToList();

        if (!filteredTriangles.Any())
            return Result<STLModel>.Fail("Filter resulted in no triangles");

        var newMetadata = Metadata with
        {
            TriangleCount = filteredTriangles.Count,
            LoadedAt = DateTime.UtcNow
        };

        return Create(newMetadata, filteredTriangles, RawData);
    }

    public override string ToString()
    {
        return $"STLModel[{Metadata.FileName}, {TriangleCount} triangles, {Metadata.Format}]";
    }
}
