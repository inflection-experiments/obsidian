using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for managing measurement session storage and retrieval.
/// </summary>
public interface IMeasurementSessionRepository
{
    /// <summary>
    /// Gets a measurement session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the session.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The measurement session if found; otherwise, null.</returns>
    Task<Result<MeasurementSession?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all measurement sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of all measurement sessions.</returns>
    Task<Result<IEnumerable<MeasurementSession>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurement sessions filtered by model ID.
    /// </summary>
    /// <param name="modelId">The model ID to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of measurement sessions for the specified model.</returns>
    Task<Result<IEnumerable<MeasurementSession>>> GetByModelIdAsync(Guid modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets measurement sessions that have at least one measurement.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of measurement sessions with measurements.</returns>
    Task<Result<IEnumerable<MeasurementSession>>> GetWithMeasurementsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a measurement session in the repository.
    /// </summary>
    /// <param name="session">The measurement session to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The saved measurement session.</returns>
    Task<Result<MeasurementSession>> SaveAsync(MeasurementSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a measurement session from the repository.
    /// </summary>
    /// <param name="id">The unique identifier of the session to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the session was removed; otherwise, false.</returns>
    Task<Result<bool>> RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a measurement session with the specified ID exists in the repository.
    /// </summary>
    /// <param name="id">The unique identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the session exists; otherwise, false.</returns>
    Task<Result<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
