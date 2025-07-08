using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for managing STL model storage and retrieval.
/// </summary>
public interface IModelRepository
{
    /// <summary>
    /// Gets an STL model by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The STL model if found; otherwise, null.</returns>
    Task<Result<STLModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all loaded STL models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of all loaded STL models.</returns>
    Task<Result<IEnumerable<STLModel>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates an STL model in the repository.
    /// </summary>
    /// <param name="model">The STL model to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The saved model.</returns>
    Task<Result<STLModel>> SaveAsync(STLModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an STL model from the repository.
    /// </summary>
    /// <param name="id">The unique identifier of the model to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the model was removed; otherwise, false.</returns>
    Task<Result<bool>> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model with the specified ID exists in the repository.
    /// </summary>
    /// <param name="id">The unique identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the model exists; otherwise, false.</returns>
    Task<Result<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
