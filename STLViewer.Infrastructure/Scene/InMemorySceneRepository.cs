using System.Collections.Concurrent;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Infrastructure.Scene;

/// <summary>
/// In-memory implementation of the scene repository.
/// </summary>
public class InMemorySceneRepository : ISceneRepository
{
    private readonly ConcurrentDictionary<Guid, Domain.Entities.Scene> _scenes = new();
    private readonly ConcurrentDictionary<Guid, SceneMetadata> _metadata = new();

    /// <summary>
    /// Saves a scene to the repository.
    /// </summary>
    /// <param name="scene">The scene to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a result indicating success or failure.</returns>
    public Task<Result> SaveAsync(Domain.Entities.Scene scene, CancellationToken cancellationToken = default)
    {
        if (scene == null)
            return Task.FromResult(Result.Fail("Scene cannot be null."));

        try
        {
            _scenes.AddOrUpdate(scene.Id, scene, (key, oldValue) => scene);

            // Update metadata
            var metadata = SceneMetadata.FromScene(scene);
            _metadata.AddOrUpdate(scene.Id, metadata, (key, oldValue) => metadata);

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Failed to save scene: {ex.Message}"));
        }
    }

    /// <summary>
    /// Loads a scene from the repository by its ID.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to load.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the loaded scene or an error.</returns>
    public Task<Result<Domain.Entities.Scene>> LoadAsync(Guid sceneId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result<Domain.Entities.Scene>.Ok(scene));
            }

            return Task.FromResult(Result<Domain.Entities.Scene>.Fail($"Scene with ID {sceneId} not found."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<Domain.Entities.Scene>.Fail($"Failed to load scene: {ex.Message}"));
        }
    }

    /// <summary>
    /// Deletes a scene from the repository.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a result indicating success or failure.</returns>
    public Task<Result> DeleteAsync(Guid sceneId, CancellationToken cancellationToken = default)
    {
        try
        {
            var removed = _scenes.TryRemove(sceneId, out _);
            _metadata.TryRemove(sceneId, out _);

            if (!removed)
                return Task.FromResult(Result.Fail($"Scene with ID {sceneId} not found."));

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Failed to delete scene: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets all scenes from the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the collection of scenes.</returns>
    public Task<Result<IEnumerable<Domain.Entities.Scene>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var scenes = _scenes.Values.ToList();
            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Ok(scenes));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Fail($"Failed to get all scenes: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets scene metadata (basic info without full object hierarchy).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the collection of scene metadata.</returns>
    public Task<Result<IEnumerable<SceneMetadata>>> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = _metadata.Values.ToList();
            return Task.FromResult(Result<IEnumerable<SceneMetadata>>.Ok(metadata));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<SceneMetadata>>.Fail($"Failed to get scene metadata: {ex.Message}"));
        }
    }

    /// <summary>
    /// Checks if a scene exists in the repository.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to check.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a boolean result.</returns>
    public Task<bool> ExistsAsync(Guid sceneId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_scenes.ContainsKey(sceneId));
    }

    /// <summary>
    /// Gets the total number of scenes in the repository.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the count.</returns>
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_scenes.Count);
    }

    /// <summary>
    /// Searches for scenes by name.
    /// </summary>
    /// <param name="searchTerm">The search term to match against scene names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the matching scenes.</returns>
    public Task<Result<IEnumerable<Domain.Entities.Scene>>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Fail("Search term cannot be null or empty."));

        try
        {
            var matchingScenes = _scenes.Values
                .Where(scene => scene.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Ok(matchingScenes));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Fail($"Failed to search scenes: {ex.Message}"));
        }
    }

    /// <summary>
    /// Gets recently modified scenes.
    /// </summary>
    /// <param name="count">The maximum number of scenes to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with the recently modified scenes.</returns>
    public Task<Result<IEnumerable<Domain.Entities.Scene>>> GetRecentlyModifiedAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            var recentScenes = _scenes.Values
                .OrderByDescending(scene => scene.LastModified)
                .Take(count)
                .ToList();

            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Ok(recentScenes));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<Domain.Entities.Scene>>.Fail($"Failed to get recently modified scenes: {ex.Message}"));
        }
    }

    /// <summary>
    /// Clears all scenes from the repository.
    /// </summary>
    public void Clear()
    {
        _scenes.Clear();
        _metadata.Clear();
    }

    /// <summary>
    /// Gets the current scene count.
    /// </summary>
    public int Count => _scenes.Count;

    /// <summary>
    /// Gets all scene IDs in the repository.
    /// </summary>
    public IEnumerable<Guid> SceneIds => _scenes.Keys;
}
