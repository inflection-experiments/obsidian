using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Queries;

/// <summary>
/// Handler for retrieving measurement sessions.
/// </summary>
public sealed class GetMeasurementSessionsQueryHandler : IRequestHandler<GetMeasurementSessionsQuery, Result<IEnumerable<MeasurementSession>>>
{
    private readonly IMeasurementSessionRepository _sessionRepository;

    /// <summary>
    /// Initializes a new instance of the GetMeasurementSessionsQueryHandler class.
    /// </summary>
    /// <param name="sessionRepository">The measurement session repository.</param>
    public GetMeasurementSessionsQueryHandler(IMeasurementSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
    }

    /// <summary>
    /// Handles the get measurement sessions query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The collection of measurement sessions.</returns>
    public async Task<Result<IEnumerable<MeasurementSession>>> Handle(GetMeasurementSessionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
                return Result<IEnumerable<MeasurementSession>>.Fail("Query cannot be null");

            IEnumerable<MeasurementSession> sessions;

            // Apply filters based on the query parameters
            if (request.ModelId.HasValue)
            {
                var modelResult = await _sessionRepository.GetByModelIdAsync(request.ModelId.Value, cancellationToken);
                if (!modelResult.IsSuccess)
                    return Result<IEnumerable<MeasurementSession>>.Fail(modelResult.Error);

                sessions = modelResult.Value;
            }
            else if (request.OnlyWithMeasurements)
            {
                var withMeasurementsResult = await _sessionRepository.GetWithMeasurementsAsync(cancellationToken);
                if (!withMeasurementsResult.IsSuccess)
                    return Result<IEnumerable<MeasurementSession>>.Fail(withMeasurementsResult.Error);

                sessions = withMeasurementsResult.Value;
            }
            else
            {
                var allResult = await _sessionRepository.GetAllAsync(cancellationToken);
                if (!allResult.IsSuccess)
                    return Result<IEnumerable<MeasurementSession>>.Fail(allResult.Error);

                sessions = allResult.Value;
            }

            // Apply additional filters
            if (request.OnlyWithMeasurements && !request.ModelId.HasValue)
            {
                sessions = sessions.Where(s => s.HasMeasurements);
            }

            // Apply limit if specified
            if (request.Limit.HasValue && request.Limit.Value > 0)
            {
                sessions = sessions.Take(request.Limit.Value);
            }

            // Sort by last modified (most recent first)
            sessions = sessions.OrderByDescending(s => s.LastModified);

            return Result.Ok(sessions);
        }
        catch (OperationCanceledException)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail("Query was cancelled");
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<MeasurementSession>>.Fail($"Failed to handle get measurement sessions query: {ex.Message}");
        }
    }
}
