using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for scene persistence and retrieval.
/// </summary>
public interface ISceneRepository
{
    /// <summary>
    /// Saves a scene to the repository.
    /// </summary>
    /// <param name="scene">The scene to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a result indicating success or failure.</returns>
    Task<Result> SaveAsync(Scene scene, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a scene from the repository by its ID.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the loaded scene or an error.</returns>
    Task<Result<Scene>> LoadAsync(Guid sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scene from the repository.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a result indicating success or failure.</returns>
    Task<Result> DeleteAsync(Guid sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scenes from the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the collection of scenes.</returns>
    Task<Result<IEnumerable<Scene>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets scene metadata (basic info without full object hierarchy).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the collection of scene metadata.</returns>
    Task<Result<IEnumerable<SceneMetadata>>> GetMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a scene exists in the repository.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean result.</returns>
    Task<bool> ExistsAsync(Guid sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of scenes in the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the count.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for scenes by name.
    /// </summary>
    /// <param name="searchTerm">The search term to match against scene names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the matching scenes.</returns>
    Task<Result<IEnumerable<Scene>>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recently modified scenes.
    /// </summary>
    /// <param name="count">The maximum number of scenes to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the recently modified scenes.</returns>
    Task<Result<IEnumerable<Scene>>> GetRecentlyModifiedAsync(int count = 10, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents metadata about a scene without the full object hierarchy.
/// </summary>
public sealed record SceneMetadata
{
    /// <summary>
    /// Gets the scene ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the scene name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the scene description.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the scene was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the scene was last modified.
    /// </summary>
    public DateTime LastModified { get; init; }

    /// <summary>
    /// Gets the total number of objects in the scene.
    /// </summary>
    public int ObjectCount { get; init; }

    /// <summary>
    /// Gets the number of layers in the scene.
    /// </summary>
    public int LayerCount { get; init; }

    /// <summary>
    /// Gets the total number of triangles in the scene.
    /// </summary>
    public int TriangleCount { get; init; }

    /// <summary>
    /// Gets the file size in bytes (for file-based storage).
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Gets a value indicating whether the scene is empty.
    /// </summary>
    public bool IsEmpty => ObjectCount == 0;

    /// <summary>
    /// Creates scene metadata from a scene.
    /// </summary>
    /// <param name="scene">The scene to create metadata from.</param>
    /// <param name="fileSizeBytes">The file size in bytes (optional).</param>
    /// <returns>The scene metadata.</returns>
    public static SceneMetadata FromScene(Scene scene, long fileSizeBytes = 0)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));

        var allNodes = scene.GetAllNodes().ToList();
        var objects = allNodes.OfType<SceneObject>().ToList();
        var triangleCount = objects.Sum(obj => obj.Model.TriangleCount);

        return new SceneMetadata
        {
            Id = scene.Id,
            Name = scene.Name,
            Description = scene.Description,
            CreatedAt = scene.CreatedAt,
            LastModified = scene.LastModified,
            ObjectCount = objects.Count,
            LayerCount = scene.Layers.Count,
            TriangleCount = triangleCount,
            FileSizeBytes = fileSizeBytes
        };
    }
}
