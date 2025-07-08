using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Infrastructure.Scene;

/// <summary>
/// Implementation of scene management functionality.
/// </summary>
public class SceneManager : ISceneManager
{
    private readonly ISceneRepository _sceneRepository;
    private Domain.Entities.Scene? _currentScene;

    /// <summary>
    /// Initializes a new instance of the SceneManager class.
    /// </summary>
    /// <param name="sceneRepository">The scene repository.</param>
    public SceneManager(ISceneRepository sceneRepository)
    {
        _sceneRepository = sceneRepository ?? throw new ArgumentNullException(nameof(sceneRepository));
    }

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public Domain.Entities.Scene? CurrentScene => _currentScene;

    /// <summary>
    /// Event raised when the current scene changes.
    /// </summary>
    public event EventHandler<SceneChangedEventArgs>? CurrentSceneChanged;

    /// <summary>
    /// Event raised when a scene is modified.
    /// </summary>
    public event EventHandler<SceneModifiedEventArgs>? SceneModified;

    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    /// <param name="name">The name of the scene.</param>
    /// <returns>A result containing the new scene or an error.</returns>
    public Result<Domain.Entities.Scene> CreateScene(string name)
    {
        try
        {
            var scene = Domain.Entities.Scene.Create(name);
            return Result<Domain.Entities.Scene>.Ok(scene);
        }
        catch (Exception ex)
        {
            return Result<Domain.Entities.Scene>.Fail($"Failed to create scene: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets the active scene.
    /// </summary>
    /// <param name="scene">The scene to set as active.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result SetCurrentScene(Domain.Entities.Scene? scene)
    {
        var previousScene = _currentScene;
        _currentScene = scene;

        CurrentSceneChanged?.Invoke(this, new SceneChangedEventArgs(previousScene, scene));

        return Result.Ok();
    }

    /// <summary>
    /// Adds an STL model to the current scene.
    /// </summary>
    /// <param name="model">The STL model to add.</param>
    /// <param name="name">The name for the scene object (optional).</param>
    /// <param name="layerId">The layer ID to add the object to (optional, uses default layer if null).</param>
    /// <param name="parent">The parent node to add the object to (optional, adds to root if null).</param>
    /// <returns>A result containing the created scene object or an error.</returns>
    public Result<SceneObject> AddModelToScene(STLModel model, string? name = null, Guid? layerId = null, SceneNode? parent = null)
    {
        if (_currentScene == null)
            return Result<SceneObject>.Fail("No active scene to add model to.");

        if (model == null)
            return Result<SceneObject>.Fail("Model cannot be null.");

        try
        {
            var objectName = name ?? $"Object_{DateTime.Now:HHmmss}";
            var targetLayerId = layerId ?? _currentScene.GetDefaultLayer().Id;

            var sceneObject = SceneObject.Create(objectName, model, targetLayerId);

            if (parent != null)
            {
                parent.AddChild(sceneObject);
            }
            else
            {
                _currentScene.AddRootNode(sceneObject);
            }

            RaiseSceneModified(SceneModificationType.NodeAdded, sceneObject.Id);

            return Result<SceneObject>.Ok(sceneObject);
        }
        catch (Exception ex)
        {
            return Result<SceneObject>.Fail($"Failed to add model to scene: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a scene node from the current scene.
    /// </summary>
    /// <param name="nodeId">The ID of the node to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveFromScene(Guid nodeId)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var node = _currentScene.FindNode(nodeId);
            if (node == null)
                return Result.Fail($"Node with ID {nodeId} not found in scene.");

            if (node.Parent != null)
            {
                node.Parent.RemoveChild(node);
            }
            else
            {
                _currentScene.RemoveRootNode(node);
            }

            RaiseSceneModified(SceneModificationType.NodeRemoved, nodeId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remove node from scene: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new group in the current scene.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    /// <param name="layerId">The layer ID for the group (optional, uses default layer if null).</param>
    /// <param name="parent">The parent node to add the group to (optional, adds to root if null).</param>
    /// <returns>A result containing the created scene group or an error.</returns>
    public Result<SceneGroup> CreateGroup(string name, Guid? layerId = null, SceneNode? parent = null)
    {
        if (_currentScene == null)
            return Result<SceneGroup>.Fail("No active scene to create group in.");

        try
        {
            var targetLayerId = layerId ?? _currentScene.GetDefaultLayer().Id;
            var group = SceneGroup.Create(name, targetLayerId);

            if (parent != null)
            {
                parent.AddChild(group);
            }
            else
            {
                _currentScene.AddRootNode(group);
            }

            RaiseSceneModified(SceneModificationType.NodeAdded, group.Id);

            return Result<SceneGroup>.Ok(group);
        }
        catch (Exception ex)
        {
            return Result<SceneGroup>.Fail($"Failed to create group: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds a child node to a parent node.
    /// </summary>
    /// <param name="parentId">The ID of the parent node.</param>
    /// <param name="childId">The ID of the child node.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result AddToParent(Guid parentId, Guid childId)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var parent = _currentScene.FindNode(parentId);
            var child = _currentScene.FindNode(childId);

            if (parent == null)
                return Result.Fail($"Parent node with ID {parentId} not found.");

            if (child == null)
                return Result.Fail($"Child node with ID {childId} not found.");

            parent.AddChild(child);

            RaiseSceneModified(SceneModificationType.NodeAdded, childId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to add child to parent: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a child node from its parent.
    /// </summary>
    /// <param name="childId">The ID of the child node.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveFromParent(Guid childId)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var child = _currentScene.FindNode(childId);
            if (child == null)
                return Result.Fail($"Child node with ID {childId} not found.");

            child.RemoveFromParent();

            RaiseSceneModified(SceneModificationType.NodeRemoved, childId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remove child from parent: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the transform of a scene node.
    /// </summary>
    /// <param name="nodeId">The ID of the node to update.</param>
    /// <param name="transform">The new transform.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateNodeTransform(Guid nodeId, Transform transform)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var node = _currentScene.FindNode(nodeId);
            if (node == null)
                return Result.Fail($"Node with ID {nodeId} not found.");

            node.SetTransform(transform);

            RaiseSceneModified(SceneModificationType.NodeTransformed, nodeId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update node transform: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the visibility of a scene node.
    /// </summary>
    /// <param name="nodeId">The ID of the node to update.</param>
    /// <param name="isVisible">The new visibility state.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateNodeVisibility(Guid nodeId, bool isVisible)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var node = _currentScene.FindNode(nodeId);
            if (node == null)
                return Result.Fail($"Node with ID {nodeId} not found.");

            node.IsVisible = isVisible;

            RaiseSceneModified(SceneModificationType.NodeVisibilityChanged, nodeId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update node visibility: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the material of a scene object.
    /// </summary>
    /// <param name="objectId">The ID of the scene object to update.</param>
    /// <param name="material">The new material.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateObjectMaterial(Guid objectId, SceneObjectMaterial material)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var node = _currentScene.FindNode(objectId);
            if (node is not SceneObject sceneObject)
                return Result.Fail($"Node with ID {objectId} is not a scene object.");

            sceneObject.SetMaterial(material);

            RaiseSceneModified(SceneModificationType.MaterialChanged, objectId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update object material: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a new layer in the current scene.
    /// </summary>
    /// <param name="name">The name of the layer.</param>
    /// <param name="description">The description of the layer (optional).</param>
    /// <param name="color">The color of the layer (optional).</param>
    /// <returns>A result containing the created layer or an error.</returns>
    public Result<Layer> CreateLayer(string name, string description = "", STLViewer.Math.Color? color = null)
    {
        if (_currentScene == null)
            return Result<Layer>.Fail("No active scene to create layer in.");

        try
        {
            var layer = Layer.Create(name, description, color);
            _currentScene.AddLayer(layer);

            RaiseSceneModified(SceneModificationType.LayerAdded);

            return Result<Layer>.Ok(layer);
        }
        catch (Exception ex)
        {
            return Result<Layer>.Fail($"Failed to create layer: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a layer from the current scene.
    /// </summary>
    /// <param name="layerId">The ID of the layer to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveLayer(Guid layerId)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var removed = _currentScene.RemoveLayer(layerId);
            if (!removed)
                return Result.Fail($"Layer with ID {layerId} not found or cannot be removed.");

            RaiseSceneModified(SceneModificationType.LayerRemoved);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to remove layer: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates layer properties.
    /// </summary>
    /// <param name="layerId">The ID of the layer to update.</param>
    /// <param name="name">The new name (optional).</param>
    /// <param name="description">The new description (optional).</param>
    /// <param name="color">The new color (optional).</param>
    /// <param name="isVisible">The new visibility (optional).</param>
    /// <param name="isSelectable">The new selectability (optional).</param>
    /// <param name="isLocked">The new locked state (optional).</param>
    /// <param name="opacity">The new opacity (optional).</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateLayer(
        Guid layerId,
        string? name = null,
        string? description = null,
        STLViewer.Math.Color? color = null,
        bool? isVisible = null,
        bool? isSelectable = null,
        bool? isLocked = null,
        float? opacity = null)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var layer = _currentScene.GetLayer(layerId);
            if (layer == null)
                return Result.Fail($"Layer with ID {layerId} not found.");

            var updatedLayer = layer.With(
                name: name,
                description: description,
                color: color,
                isVisible: isVisible,
                isSelectable: isSelectable,
                isLocked: isLocked,
                opacity: opacity);

            // Remove old layer and add updated one
            _currentScene.RemoveLayer(layerId);
            _currentScene.AddLayer(updatedLayer);

            RaiseSceneModified(SceneModificationType.LayerModified);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to update layer: {ex.Message}");
        }
    }

    /// <summary>
    /// Moves a scene node to a different layer.
    /// </summary>
    /// <param name="nodeId">The ID of the node to move.</param>
    /// <param name="layerId">The ID of the target layer.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result MoveToLayer(Guid nodeId, Guid layerId)
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            var node = _currentScene.FindNode(nodeId);
            if (node == null)
                return Result.Fail($"Node with ID {nodeId} not found.");

            var layer = _currentScene.GetLayer(layerId);
            if (layer == null)
                return Result.Fail($"Layer with ID {layerId} not found.");

            node.SetLayer(layerId);

            RaiseSceneModified(SceneModificationType.NodeMovedToLayer, nodeId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to move node to layer: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all nodes in a specific layer.
    /// </summary>
    /// <param name="layerId">The ID of the layer.</param>
    /// <returns>A collection of nodes in the specified layer.</returns>
    public IEnumerable<SceneNode> GetNodesInLayer(Guid layerId)
    {
        if (_currentScene == null)
            return Enumerable.Empty<SceneNode>();

        return _currentScene.GetAllNodes().Where(node => node.LayerId == layerId);
    }

    /// <summary>
    /// Gets the scene statistics (number of objects, triangles, etc.).
    /// </summary>
    /// <returns>A result containing the scene statistics or an error.</returns>
    public Result<SceneStatistics> GetSceneStatistics()
    {
        if (_currentScene == null)
            return Result<SceneStatistics>.Fail("No active scene.");

        try
        {
            var statistics = SceneStatistics.Calculate(_currentScene);
            return Result<SceneStatistics>.Ok(statistics);
        }
        catch (Exception ex)
        {
            return Result<SceneStatistics>.Fail($"Failed to calculate scene statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears the current scene.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result ClearScene()
    {
        if (_currentScene == null)
            return Result.Fail("No active scene.");

        try
        {
            _currentScene.Clear();

            RaiseSceneModified(SceneModificationType.SceneCleared);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to clear scene: {ex.Message}");
        }
    }

    private void RaiseSceneModified(SceneModificationType modificationType, Guid? affectedNodeId = null)
    {
        if (_currentScene != null)
        {
            SceneModified?.Invoke(this, new SceneModifiedEventArgs(_currentScene, modificationType, affectedNodeId));
        }
    }
}
