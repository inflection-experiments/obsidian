using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Domain.Entities;

/// <summary>
/// Represents a 3D scene containing multiple objects organized in a hierarchical structure.
/// </summary>
public class Scene : Entity<Guid>
{
    private readonly List<SceneNode> _rootNodes = new();
    private readonly List<Layer> _layers = new();

    /// <summary>
    /// Gets the unique identifier for this scene.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets or sets the name of the scene.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the scene.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the scene was created.
    /// </summary>
    public new DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when the scene was last modified.
    /// </summary>
    public DateTime LastModified { get; private set; }

    /// <summary>
    /// Gets the read-only collection of root nodes in the scene.
    /// </summary>
    public IReadOnlyList<SceneNode> RootNodes => _rootNodes.AsReadOnly();

    /// <summary>
    /// Gets the read-only collection of layers in the scene.
    /// </summary>
    public IReadOnlyList<Layer> Layers => _layers.AsReadOnly();

    /// <summary>
    /// Gets the total number of objects in the scene (including child objects).
    /// </summary>
    public int TotalObjectCount => CountAllObjects(_rootNodes);

    /// <summary>
    /// Gets a value indicating whether the scene is empty.
    /// </summary>
    public bool IsEmpty => _rootNodes.Count == 0;

    private Scene(Guid id, string name)
    {
        Id = id;
        Name = name;
        CreatedAt = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;

        // Create default layer
        _layers.Add(Layer.CreateDefault());
    }

    /// <summary>
    /// Creates a new scene with the specified name.
    /// </summary>
    /// <param name="name">The name of the scene.</param>
    /// <returns>A new scene instance.</returns>
    public static Scene Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scene name cannot be null or empty.", nameof(name));

        return new Scene(Guid.NewGuid(), name);
    }

    /// <summary>
    /// Adds a scene node to the root level of the scene.
    /// </summary>
    /// <param name="node">The scene node to add.</param>
    public void AddRootNode(SceneNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        if (_rootNodes.Contains(node))
            return;

        _rootNodes.Add(node);
        node.SetParent(null);
        node.SetScene(this);
        UpdateLastModified();
    }

    /// <summary>
    /// Removes a scene node from the root level of the scene.
    /// </summary>
    /// <param name="node">The scene node to remove.</param>
    /// <returns>True if the node was removed; otherwise, false.</returns>
    public bool RemoveRootNode(SceneNode node)
    {
        if (node == null)
            return false;

        var removed = _rootNodes.Remove(node);
        if (removed)
        {
            node.SetScene(null);
            UpdateLastModified();
        }

        return removed;
    }

    /// <summary>
    /// Adds a new layer to the scene.
    /// </summary>
    /// <param name="layer">The layer to add.</param>
    public void AddLayer(Layer layer)
    {
        if (layer == null)
            throw new ArgumentNullException(nameof(layer));

        if (_layers.Any(l => l.Id == layer.Id))
            return;

        _layers.Add(layer);
        UpdateLastModified();
    }

    /// <summary>
    /// Removes a layer from the scene.
    /// </summary>
    /// <param name="layerId">The ID of the layer to remove.</param>
    /// <returns>True if the layer was removed; otherwise, false.</returns>
    public bool RemoveLayer(Guid layerId)
    {
        var layer = _layers.FirstOrDefault(l => l.Id == layerId);
        if (layer == null || layer.IsDefault)
            return false;

        // Move all objects from this layer to the default layer
        var defaultLayer = _layers.First(l => l.IsDefault);
        var allNodes = GetAllNodes();
        foreach (var node in allNodes.Where(n => n.LayerId == layerId))
        {
            node.SetLayer(defaultLayer.Id);
        }

        var removed = _layers.Remove(layer);
        if (removed)
            UpdateLastModified();

        return removed;
    }

    /// <summary>
    /// Gets a layer by its ID.
    /// </summary>
    /// <param name="layerId">The ID of the layer.</param>
    /// <returns>The layer if found; otherwise, null.</returns>
    public Layer? GetLayer(Guid layerId)
    {
        return _layers.FirstOrDefault(l => l.Id == layerId);
    }

    /// <summary>
    /// Gets the default layer.
    /// </summary>
    /// <returns>The default layer.</returns>
    public Layer GetDefaultLayer()
    {
        return _layers.First(l => l.IsDefault);
    }

    /// <summary>
    /// Gets all nodes in the scene (including child nodes).
    /// </summary>
    /// <returns>A collection of all scene nodes.</returns>
    public IEnumerable<SceneNode> GetAllNodes()
    {
        return GetAllNodesRecursive(_rootNodes);
    }

    /// <summary>
    /// Finds a node by its ID.
    /// </summary>
    /// <param name="nodeId">The ID of the node to find.</param>
    /// <returns>The node if found; otherwise, null.</returns>
    public SceneNode? FindNode(Guid nodeId)
    {
        return GetAllNodes().FirstOrDefault(n => n.Id == nodeId);
    }

    /// <summary>
    /// Clears all objects from the scene.
    /// </summary>
    public void Clear()
    {
        _rootNodes.Clear();

        // Keep default layer, remove others
        var defaultLayer = _layers.First(l => l.IsDefault);
        _layers.Clear();
        _layers.Add(defaultLayer);

        UpdateLastModified();
    }

    private void UpdateLastModified()
    {
        LastModified = DateTime.UtcNow;
    }

    private static int CountAllObjects(IEnumerable<SceneNode> nodes)
    {
        return nodes.Sum(node => 1 + CountAllObjects(node.Children));
    }

    private static IEnumerable<SceneNode> GetAllNodesRecursive(IEnumerable<SceneNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in GetAllNodesRecursive(node.Children))
            {
                yield return child;
            }
        }
    }
}
