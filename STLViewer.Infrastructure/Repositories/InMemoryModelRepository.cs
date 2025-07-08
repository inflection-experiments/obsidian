using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using System.Collections.Concurrent;

namespace STLViewer.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the model repository.
/// </summary>
public class InMemoryModelRepository : IModelRepository
{
    private readonly ConcurrentDictionary<Guid, STLModel> _models = new();

    /// <summary>
    /// Gets an STL model by its unique identifier.
    /// </summary>
    public async Task<Result<STLModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var model = _models.TryGetValue(id, out var foundModel) ? foundModel : null;
            return Result.Ok(model);
        }
        catch (OperationCanceledException)
        {
            return Result<STLModel?>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<STLModel?>.Fail($"Failed to retrieve model: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all loaded STL models.
    /// </summary>
    public async Task<Result<IEnumerable<STLModel>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var models = _models.Values.ToList();
            return Result.Ok((IEnumerable<STLModel>)models);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<STLModel>>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<STLModel>>.Fail($"Failed to retrieve models: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds or updates an STL model in the repository.
    /// </summary>
    public async Task<Result<STLModel>> SaveAsync(STLModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<STLModel>.Fail("Model cannot be null");

            await Task.Delay(0, cancellationToken); // Simulate async operation

            _models.AddOrUpdate(model.Id, model, (key, existing) => model);
            return Result.Ok(model);
        }
        catch (OperationCanceledException)
        {
            return Result<STLModel>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<STLModel>.Fail($"Failed to save model: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes an STL model from the repository.
    /// </summary>
    public async Task<Result<bool>> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var removed = _models.TryRemove(id, out _);
            return Result.Ok(removed);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to remove model: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a model with the specified ID exists in the repository.
    /// </summary>
    public async Task<Result<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var exists = _models.ContainsKey(id);
            return Result.Ok(exists);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to check model existence: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current number of models in the repository.
    /// </summary>
    public int Count => _models.Count;

    /// <summary>
    /// Clears all models from the repository.
    /// </summary>
    public void Clear()
    {
        _models.Clear();
    }
}
