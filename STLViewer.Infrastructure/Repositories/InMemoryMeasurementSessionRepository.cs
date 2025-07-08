using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace STLViewer.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the measurement session repository.
/// </summary>
public class InMemoryMeasurementSessionRepository : IMeasurementSessionRepository
{
    private readonly ConcurrentDictionary<Guid, MeasurementSession> _sessions = new();

    /// <summary>
    /// Gets a measurement session by its unique identifier.
    /// </summary>
    public async Task<Result<MeasurementSession?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var session = _sessions.TryGetValue(id, out var foundSession) ? foundSession : null;
            return Result.Ok(session);
        }
        catch (OperationCanceledException)
        {
            return Result<MeasurementSession?>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<MeasurementSession?>.Fail($"Failed to retrieve session: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all measurement sessions.
    /// </summary>
    public async Task<Result<IEnumerable<MeasurementSession>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var sessions = _sessions.Values.ToList();
            return Result.Ok((IEnumerable<MeasurementSession>)sessions);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail($"Failed to retrieve sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets measurement sessions filtered by model ID.
    /// </summary>
    public async Task<Result<IEnumerable<MeasurementSession>>> GetByModelIdAsync(Guid modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            // For this simple implementation, we'll filter by session name containing the model ID
            // In a real implementation, you'd store model ID as a property of the session
            var sessions = _sessions.Values
                .Where(s => s.Name.Contains(modelId.ToString(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Result.Ok((IEnumerable<MeasurementSession>)sessions);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail($"Failed to retrieve sessions by model ID: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets measurement sessions that have at least one measurement.
    /// </summary>
    public async Task<Result<IEnumerable<MeasurementSession>>> GetWithMeasurementsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var sessions = _sessions.Values
                .Where(s => s.HasMeasurements)
                .ToList();

            return Result.Ok((IEnumerable<MeasurementSession>)sessions);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail($"Failed to retrieve sessions with measurements: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds or updates a measurement session in the repository.
    /// </summary>
    public async Task<Result<MeasurementSession>> SaveAsync(MeasurementSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            if (session == null)
                return Result<MeasurementSession>.Fail("Session cannot be null");

            await Task.Delay(0, cancellationToken); // Simulate async operation

            _sessions.AddOrUpdate(session.Id, session, (key, existing) => session);
            return Result.Ok(session);
        }
        catch (OperationCanceledException)
        {
            return Result<MeasurementSession>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<MeasurementSession>.Fail($"Failed to save session: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes a measurement session from the repository.
    /// </summary>
    public async Task<Result<bool>> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var removed = _sessions.TryRemove(id, out _);
            return Result.Ok(removed);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to remove session: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a measurement session with the specified ID exists in the repository.
    /// </summary>
    public async Task<Result<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(0, cancellationToken); // Simulate async operation

            var exists = _sessions.ContainsKey(id);
            return Result.Ok(exists);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Fail("Operation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to check session existence: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the current number of sessions in the repository.
    /// </summary>
    public int Count => _sessions.Count;

    /// <summary>
    /// Clears all sessions from the repository.
    /// </summary>
    public void Clear()
    {
        _sessions.Clear();
    }
}
