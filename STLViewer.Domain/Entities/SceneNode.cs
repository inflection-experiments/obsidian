using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Domain.Entities;

/// <summary>
/// Represents a node in the scene graph hierarchy.
/// </summary>
public abstract class SceneNode : Entity<Guid>
{
    private readonly List<SceneNode> _children = new();
    private SceneNode? _parent;
    private Scene? _scene;

    /// <summary>
    /// Gets the unique identifier for this scene node.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets or sets the name of the scene node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this node is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this node is enabled for interaction.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the transformation of this node.
    /// </summary>
    public Transform Transform { get; private set; }

    /// <summary>
    /// Gets the layer ID this node belongs to.
    /// </summary>
    public Guid LayerId { get; private set; }

    /// <summary>
    /// Gets the parent node of this node.
    /// </summary>
    public SceneNode? Parent => _parent;

    /// <summary>
    /// Gets the scene this node belongs to.
    /// </summary>
    public Scene? Scene => _scene;

    /// <summary>
    /// Gets the read-only collection of child nodes.
    /// </summary>
    public IReadOnlyList<SceneNode> Children => _children.AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether this node has children.
    /// </summary>
    public bool HasChildren => _children.Count > 0;

    /// <summary>
    /// Gets the depth of this node in the hierarchy (0 for root nodes).
    /// </summary>
    public int Depth
    {
        get
        {
            var depth = 0;
            var current = _parent;
            while (current != null)
            {
                depth++;
                current = current._parent;
            }
            return depth;
        }
    }

    /// <summary>
    /// Gets the world transformation by combining this node's transform with all parent transforms.
    /// </summary>
    public Transform WorldTransform
    {
        get
        {
            if (_parent == null)
                return Transform;

            return Transform.CombineWith(_parent.WorldTransform);
        }
    }

    protected SceneNode(string name, Guid layerId)
    {
        Id = Guid.NewGuid();
        Name = name;
        LayerId = layerId;
        Transform = Transform.Identity;
    }

    /// <summary>
    /// Adds a child node to this node.
    /// </summary>
    /// <param name="child">The child node to add.</param>
    public void AddChild(SceneNode child)
    {
        if (child == null)
            throw new ArgumentNullException(nameof(child));

        if (child == this)
            throw new InvalidOperationException("Cannot add a node as a child of itself.");

        if (IsAncestorOf(child))
            throw new InvalidOperationException("Cannot add an ancestor as a child (would create a cycle).");

        if (_children.Contains(child))
            return;

        // Remove from previous parent if any
        child._parent?.RemoveChild(child);

        _children.Add(child);
        child._parent = this;
        child._scene = _scene;

        // Update scene for all descendants
        UpdateSceneForDescendants(child, _scene);
    }

    /// <summary>
    /// Removes a child node from this node.
    /// </summary>
    /// <param name="child">The child node to remove.</param>
    /// <returns>True if the child was removed; otherwise, false.</returns>
    public bool RemoveChild(SceneNode child)
    {
        if (child == null)
            return false;

        var removed = _children.Remove(child);
        if (removed)
        {
            child._parent = null;
        }

        return removed;
    }

    /// <summary>
    /// Removes this node from its parent.
    /// </summary>
    public void RemoveFromParent()
    {
        _parent?.RemoveChild(this);
    }

    /// <summary>
    /// Sets the layer for this node.
    /// </summary>
    /// <param name="layerId">The ID of the layer.</param>
    public void SetLayer(Guid layerId)
    {
        LayerId = layerId;
    }

    /// <summary>
    /// Sets the transform for this node.
    /// </summary>
    /// <param name="transform">The new transform.</param>
    public void SetTransform(Transform transform)
    {
        Transform = transform ?? throw new ArgumentNullException(nameof(transform));
    }

    /// <summary>
    /// Checks if this node is an ancestor of the specified node.
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns>True if this node is an ancestor; otherwise, false.</returns>
    public bool IsAncestorOf(SceneNode node)
    {
        var current = node._parent;
        while (current != null)
        {
            if (current == this)
                return true;
            current = current._parent;
        }
        return false;
    }

    /// <summary>
    /// Gets all descendant nodes (children, grandchildren, etc.).
    /// </summary>
    /// <returns>A collection of all descendant nodes.</returns>
    public IEnumerable<SceneNode> GetAllDescendants()
    {
        foreach (var child in _children)
        {
            yield return child;
            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Sets the parent node (internal use only).
    /// </summary>
    /// <param name="parent">The parent node.</param>
    internal void SetParent(SceneNode? parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Sets the scene (internal use only).
    /// </summary>
    /// <param name="scene">The scene.</param>
    internal void SetScene(Scene? scene)
    {
        _scene = scene;

        // Update all children
        foreach (var child in _children)
        {
            child.SetScene(scene);
        }
    }

    private void UpdateSceneForDescendants(SceneNode node, Scene? scene)
    {
        foreach (var child in node._children)
        {
            child._scene = scene;
            UpdateSceneForDescendants(child, scene);
        }
    }
}

/// <summary>
/// Represents a scene object that wraps an STL model.
/// </summary>
public class SceneObject : SceneNode
{
    /// <summary>
    /// Gets the STL model associated with this scene object.
    /// </summary>
    public STLModel Model { get; private set; }

    /// <summary>
    /// Gets or sets the material properties for rendering this object.
    /// </summary>
    public SceneObjectMaterial Material { get; set; }

    /// <summary>
    /// Gets the bounding box of this object in local space.
    /// </summary>
    public BoundingBox LocalBoundingBox => Model.BoundingBox;

    /// <summary>
    /// Gets the bounding box of this object in world space.
    /// </summary>
    public BoundingBox WorldBoundingBox => LocalBoundingBox.Transform(WorldTransform.Matrix);

    private SceneObject(string name, STLModel model, Guid layerId) : base(name, layerId)
    {
        Model = model;
        Material = SceneObjectMaterial.Default;
    }

    /// <summary>
    /// Creates a new scene object from an STL model.
    /// </summary>
    /// <param name="name">The name of the scene object.</param>
    /// <param name="model">The STL model.</param>
    /// <param name="layerId">The layer ID.</param>
    /// <returns>A new scene object.</returns>
    public static SceneObject Create(string name, STLModel model, Guid layerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scene object name cannot be null or empty.", nameof(name));

        if (model == null)
            throw new ArgumentNullException(nameof(model));

        return new SceneObject(name, model, layerId);
    }

    /// <summary>
    /// Sets the material for this scene object.
    /// </summary>
    /// <param name="material">The material to set.</param>
    public void SetMaterial(SceneObjectMaterial material)
    {
        Material = material ?? throw new ArgumentNullException(nameof(material));
    }
}

/// <summary>
/// Represents a group node that can contain other scene nodes.
/// </summary>
public class SceneGroup : SceneNode
{
    /// <summary>
    /// Gets the combined bounding box of all children in local space.
    /// </summary>
    public BoundingBox LocalBoundingBox
    {
        get
        {
            if (!HasChildren)
                return BoundingBox.Empty;

            var bounds = BoundingBox.Empty;
            foreach (var child in Children)
            {
                var childBounds = child switch
                {
                    SceneObject obj => obj.LocalBoundingBox,
                    SceneGroup group => group.LocalBoundingBox,
                    _ => BoundingBox.Empty
                };

                if (!childBounds.IsEmpty)
                {
                    bounds = bounds.IsEmpty ? childBounds : bounds.Union(childBounds);
                }
            }

            return bounds;
        }
    }

    /// <summary>
    /// Gets the combined bounding box of all children in world space.
    /// </summary>
    public BoundingBox WorldBoundingBox => LocalBoundingBox.Transform(WorldTransform.Matrix);

    private SceneGroup(string name, Guid layerId) : base(name, layerId)
    {
    }

    /// <summary>
    /// Creates a new scene group.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    /// <param name="layerId">The layer ID.</param>
    /// <returns>A new scene group.</returns>
    public static SceneGroup Create(string name, Guid layerId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Scene group name cannot be null or empty.", nameof(name));

        return new SceneGroup(name, layerId);
    }
}
