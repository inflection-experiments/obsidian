using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for managing 3D scenes.
/// </summary>
public interface ISceneManager
{
    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    Scene? CurrentScene { get; }

    /// <summary>
    /// Event raised when the current scene changes.
    /// </summary>
    event EventHandler<SceneChangedEventArgs>? CurrentSceneChanged;

    /// <summary>
    /// Event raised when a scene is modified.
    /// </summary>
    event EventHandler<SceneModifiedEventArgs>? SceneModified;

    /// <summary>
    /// Creates a new empty scene.
    /// </summary>
    /// <param name="name">The name of the scene.</param>
    /// <returns>A result containing the new scene or an error.</returns>
    Result<Scene> CreateScene(string name);

    /// <summary>
    /// Sets the active scene.
    /// </summary>
    /// <param name="scene">The scene to set as active.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result SetCurrentScene(Scene? scene);

    /// <summary>
    /// Adds an STL model to the current scene.
    /// </summary>
    /// <param name="model">The STL model to add.</param>
    /// <param name="name">The name for the scene object (optional).</param>
    /// <param name="layerId">The layer ID to add the object to (optional, uses default layer if null).</param>
    /// <param name="parent">The parent node to add the object to (optional, adds to root if null).</param>
    /// <returns>A result containing the created scene object or an error.</returns>
    Result<SceneObject> AddModelToScene(STLModel model, string? name = null, Guid? layerId = null, SceneNode? parent = null);

    /// <summary>
    /// Removes a scene node from the current scene.
    /// </summary>
    /// <param name="nodeId">The ID of the node to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result RemoveFromScene(Guid nodeId);

    /// <summary>
    /// Creates a new group in the current scene.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    /// <param name="layerId">The layer ID for the group (optional, uses default layer if null).</param>
    /// <param name="parent">The parent node to add the group to (optional, adds to root if null).</param>
    /// <returns>A result containing the created scene group or an error.</returns>
    Result<SceneGroup> CreateGroup(string name, Guid? layerId = null, SceneNode? parent = null);

    /// <summary>
    /// Adds a child node to a parent node.
    /// </summary>
    /// <param name="parentId">The ID of the parent node.</param>
    /// <param name="childId">The ID of the child node.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result AddToParent(Guid parentId, Guid childId);

    /// <summary>
    /// Removes a child node from its parent.
    /// </summary>
    /// <param name="childId">The ID of the child node.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result RemoveFromParent(Guid childId);

    /// <summary>
    /// Updates the transform of a scene node.
    /// </summary>
    /// <param name="nodeId">The ID of the node to update.</param>
    /// <param name="transform">The new transform.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result UpdateNodeTransform(Guid nodeId, Transform transform);

    /// <summary>
    /// Updates the visibility of a scene node.
    /// </summary>
    /// <param name="nodeId">The ID of the node to update.</param>
    /// <param name="isVisible">The new visibility state.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result UpdateNodeVisibility(Guid nodeId, bool isVisible);

    /// <summary>
    /// Updates the material of a scene object.
    /// </summary>
    /// <param name="objectId">The ID of the scene object to update.</param>
    /// <param name="material">The new material.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result UpdateObjectMaterial(Guid objectId, SceneObjectMaterial material);

    /// <summary>
    /// Creates a new layer in the current scene.
    /// </summary>
    /// <param name="name">The name of the layer.</param>
    /// <param name="description">The description of the layer (optional).</param>
    /// <param name="color">The color of the layer (optional).</param>
    /// <returns>A result containing the created layer or an error.</returns>
    Result<Layer> CreateLayer(string name, string description = "", STLViewer.Math.Color? color = null);

    /// <summary>
    /// Removes a layer from the current scene.
    /// </summary>
    /// <param name="layerId">The ID of the layer to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result RemoveLayer(Guid layerId);

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
    Result UpdateLayer(
        Guid layerId,
        string? name = null,
        string? description = null,
        STLViewer.Math.Color? color = null,
        bool? isVisible = null,
        bool? isSelectable = null,
        bool? isLocked = null,
        float? opacity = null);

    /// <summary>
    /// Moves a scene node to a different layer.
    /// </summary>
    /// <param name="nodeId">The ID of the node to move.</param>
    /// <param name="layerId">The ID of the target layer.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result MoveToLayer(Guid nodeId, Guid layerId);

    /// <summary>
    /// Gets all nodes in a specific layer.
    /// </summary>
    /// <param name="layerId">The ID of the layer.</param>
    /// <returns>A collection of nodes in the specified layer.</returns>
    IEnumerable<SceneNode> GetNodesInLayer(Guid layerId);

    /// <summary>
    /// Gets the scene statistics (number of objects, triangles, etc.).
    /// </summary>
    /// <returns>A result containing the scene statistics or an error.</returns>
    Result<SceneStatistics> GetSceneStatistics();

    /// <summary>
    /// Clears the current scene.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result ClearScene();
}

/// <summary>
/// Event arguments for scene changed events.
/// </summary>
public class SceneChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous scene.
    /// </summary>
    public Scene? PreviousScene { get; }

    /// <summary>
    /// Gets the new current scene.
    /// </summary>
    public Scene? NewScene { get; }

    /// <summary>
    /// Initializes a new instance of the SceneChangedEventArgs class.
    /// </summary>
    /// <param name="previousScene">The previous scene.</param>
    /// <param name="newScene">The new scene.</param>
    public SceneChangedEventArgs(Scene? previousScene, Scene? newScene)
    {
        PreviousScene = previousScene;
        NewScene = newScene;
    }
}

/// <summary>
/// Event arguments for scene modified events.
/// </summary>
public class SceneModifiedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the modified scene.
    /// </summary>
    public Scene Scene { get; }

    /// <summary>
    /// Gets the type of modification.
    /// </summary>
    public SceneModificationType ModificationType { get; }

    /// <summary>
    /// Gets the ID of the affected node (if applicable).
    /// </summary>
    public Guid? AffectedNodeId { get; }

    /// <summary>
    /// Initializes a new instance of the SceneModifiedEventArgs class.
    /// </summary>
    /// <param name="scene">The modified scene.</param>
    /// <param name="modificationType">The type of modification.</param>
    /// <param name="affectedNodeId">The ID of the affected node (optional).</param>
    public SceneModifiedEventArgs(Scene scene, SceneModificationType modificationType, Guid? affectedNodeId = null)
    {
        Scene = scene;
        ModificationType = modificationType;
        AffectedNodeId = affectedNodeId;
    }
}

/// <summary>
/// Types of scene modifications.
/// </summary>
public enum SceneModificationType
{
    NodeAdded,
    NodeRemoved,
    NodeTransformed,
    NodeVisibilityChanged,
    MaterialChanged,
    LayerAdded,
    LayerRemoved,
    LayerModified,
    NodeMovedToLayer,
    SceneCleared
}
