using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents statistical information about a scene.
/// </summary>
public sealed record SceneStatistics
{
    /// <summary>
    /// Gets the total number of objects in the scene.
    /// </summary>
    public int TotalObjects { get; init; }

    /// <summary>
    /// Gets the total number of groups in the scene.
    /// </summary>
    public int TotalGroups { get; init; }

    /// <summary>
    /// Gets the total number of triangles across all objects.
    /// </summary>
    public int TotalTriangles { get; init; }

    /// <summary>
    /// Gets the total number of vertices across all objects.
    /// </summary>
    public int TotalVertices { get; init; }

    /// <summary>
    /// Gets the number of layers in the scene.
    /// </summary>
    public int LayerCount { get; init; }

    /// <summary>
    /// Gets the maximum depth of the scene hierarchy.
    /// </summary>
    public int MaxDepth { get; init; }

    /// <summary>
    /// Gets the combined bounding box of all objects in the scene.
    /// </summary>
    public BoundingBox SceneBounds { get; init; } = BoundingBox.Empty;

    /// <summary>
    /// Gets the estimated memory usage in bytes.
    /// </summary>
    public long EstimatedMemoryUsage { get; init; }

    /// <summary>
    /// Gets the number of visible objects.
    /// </summary>
    public int VisibleObjects { get; init; }

    /// <summary>
    /// Gets the number of hidden objects.
    /// </summary>
    public int HiddenObjects { get; init; }

    /// <summary>
    /// Gets the number of transparent objects.
    /// </summary>
    public int TransparentObjects { get; init; }

    /// <summary>
    /// Gets the number of opaque objects.
    /// </summary>
    public int OpaqueObjects { get; init; }

    /// <summary>
    /// Gets statistics per layer.
    /// </summary>
    public IReadOnlyDictionary<Guid, LayerStatistics> LayerStatistics { get; init; } =
        new Dictionary<Guid, LayerStatistics>();

    /// <summary>
    /// Gets the timestamp when these statistics were calculated.
    /// </summary>
    public DateTime CalculatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a value indicating whether the scene is empty.
    /// </summary>
    public bool IsEmpty => TotalObjects == 0;

    /// <summary>
    /// Gets the average number of triangles per object.
    /// </summary>
    public double AverageTrianglesPerObject => TotalObjects > 0 ? (double)TotalTriangles / TotalObjects : 0.0;

    /// <summary>
    /// Gets the percentage of visible objects.
    /// </summary>
    public double VisibilityPercentage => TotalObjects > 0 ? (double)VisibleObjects / TotalObjects * 100.0 : 0.0;

    /// <summary>
    /// Gets the percentage of transparent objects.
    /// </summary>
    public double TransparencyPercentage => TotalObjects > 0 ? (double)TransparentObjects / TotalObjects * 100.0 : 0.0;

    private SceneStatistics() { }

    /// <summary>
    /// Creates scene statistics from a scene.
    /// </summary>
    /// <param name="scene">The scene to analyze.</param>
    /// <returns>The calculated statistics.</returns>
    public static SceneStatistics Calculate(Entities.Scene scene)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));

        var allNodes = scene.GetAllNodes().ToList();
        var objects = allNodes.OfType<Entities.SceneObject>().ToList();
        var groups = allNodes.OfType<Entities.SceneGroup>().ToList();

        var totalTriangles = objects.Sum(obj => obj.Model.TriangleCount);
        var totalVertices = totalTriangles * 3; // Each triangle has 3 vertices

        var visibleObjects = objects.Count(obj => obj.IsVisible);
        var hiddenObjects = objects.Count - visibleObjects;

        var transparentObjects = objects.Count(obj => obj.Material.IsTransparent);
        var opaqueObjects = objects.Count - transparentObjects;

        var maxDepth = allNodes.Any() ? allNodes.Max(node => node.Depth) : 0;

        // Calculate combined bounding box
        var sceneBounds = BoundingBox.Empty;
        foreach (var obj in objects.Where(o => o.IsVisible))
        {
            var objBounds = obj.WorldBoundingBox;
            sceneBounds = sceneBounds.IsEmpty ? objBounds : sceneBounds.Union(objBounds);
        }

        // Estimate memory usage (rough calculation)
        var estimatedMemoryUsage = objects.Sum(obj => EstimateObjectMemoryUsage(obj));

        // Calculate layer statistics
        var layerStats = new Dictionary<Guid, LayerStatistics>();
        foreach (var layer in scene.Layers)
        {
            var layerNodes = allNodes.Where(node => node.LayerId == layer.Id).ToList();
            var layerObjects = layerNodes.OfType<Entities.SceneObject>().ToList();

            layerStats[layer.Id] = Domain.ValueObjects.LayerStatistics.Calculate(layer, layerObjects);
        }

        return new SceneStatistics
        {
            TotalObjects = objects.Count,
            TotalGroups = groups.Count,
            TotalTriangles = totalTriangles,
            TotalVertices = totalVertices,
            LayerCount = scene.Layers.Count,
            MaxDepth = maxDepth,
            SceneBounds = sceneBounds,
            EstimatedMemoryUsage = estimatedMemoryUsage,
            VisibleObjects = visibleObjects,
            HiddenObjects = hiddenObjects,
            TransparentObjects = transparentObjects,
            OpaqueObjects = opaqueObjects,
            LayerStatistics = layerStats,
            CalculatedAt = DateTime.UtcNow
        };
    }

    private static long EstimateObjectMemoryUsage(Entities.SceneObject obj)
    {
        // Rough estimation: each triangle uses approximately 72 bytes
        // (3 vertices * 3 floats * 4 bytes + normal vector * 3 floats * 4 bytes)
        return obj.Model.TriangleCount * 72L;
    }
}

/// <summary>
/// Represents statistical information about a layer.
/// </summary>
public sealed record LayerStatistics
{
    /// <summary>
    /// Gets the layer ID.
    /// </summary>
    public Guid LayerId { get; init; }

    /// <summary>
    /// Gets the layer name.
    /// </summary>
    public string LayerName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of objects in this layer.
    /// </summary>
    public int ObjectCount { get; init; }

    /// <summary>
    /// Gets the total number of triangles in this layer.
    /// </summary>
    public int TriangleCount { get; init; }

    /// <summary>
    /// Gets the number of visible objects in this layer.
    /// </summary>
    public int VisibleObjectCount { get; init; }

    /// <summary>
    /// Gets the bounding box of all objects in this layer.
    /// </summary>
    public BoundingBox LayerBounds { get; init; } = BoundingBox.Empty;

    /// <summary>
    /// Gets a value indicating whether the layer is visible.
    /// </summary>
    public bool IsLayerVisible { get; init; }

    /// <summary>
    /// Gets a value indicating whether the layer is locked.
    /// </summary>
    public bool IsLayerLocked { get; init; }

    /// <summary>
    /// Gets the estimated memory usage of objects in this layer.
    /// </summary>
    public long EstimatedMemoryUsage { get; init; }

    private LayerStatistics() { }

    /// <summary>
    /// Calculates statistics for a layer.
    /// </summary>
    /// <param name="layer">The layer to analyze.</param>
    /// <param name="objects">The objects in the layer.</param>
    /// <returns>The calculated layer statistics.</returns>
    public static LayerStatistics Calculate(Layer layer, IEnumerable<Entities.SceneObject> objects)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));

        var objectList = objects?.ToList() ?? new List<Entities.SceneObject>();

        var triangleCount = objectList.Sum(obj => obj.Model.TriangleCount);
        var visibleObjectCount = objectList.Count(obj => obj.IsVisible);

        // Calculate layer bounding box
        var layerBounds = BoundingBox.Empty;
        foreach (var obj in objectList.Where(o => o.IsVisible))
        {
            var objBounds = obj.WorldBoundingBox;
            layerBounds = layerBounds.IsEmpty ? objBounds : layerBounds.Union(objBounds);
        }

        var estimatedMemoryUsage = objectList.Sum(obj => obj.Model.TriangleCount * 72L);

        return new LayerStatistics
        {
            LayerId = layer.Id,
            LayerName = layer.Name,
            ObjectCount = objectList.Count,
            TriangleCount = triangleCount,
            VisibleObjectCount = visibleObjectCount,
            LayerBounds = layerBounds,
            IsLayerVisible = layer.IsVisible,
            IsLayerLocked = layer.IsLocked,
            EstimatedMemoryUsage = estimatedMemoryUsage
        };
    }
}
