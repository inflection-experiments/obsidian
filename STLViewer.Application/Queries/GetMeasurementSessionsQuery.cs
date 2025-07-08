using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get all measurement sessions.
/// </summary>
public sealed record GetMeasurementSessionsQuery : IRequest<Result<IEnumerable<MeasurementSession>>>
{
    /// <summary>
    /// Filter sessions by model ID if specified.
    /// </summary>
    public Guid? ModelId { get; init; }

    /// <summary>
    /// Include only sessions with measurements if true.
    /// </summary>
    public bool OnlyWithMeasurements { get; init; } = false;

    /// <summary>
    /// Maximum number of sessions to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Creates a new query for all measurement sessions.
    /// </summary>
    public GetMeasurementSessionsQuery() { }

    /// <summary>
    /// Creates a new query filtered by model ID.
    /// </summary>
    /// <param name="modelId">The model ID to filter by.</param>
    /// <param name="onlyWithMeasurements">Whether to include only sessions with measurements.</param>
    /// <param name="limit">Maximum number of sessions to return.</param>
    public GetMeasurementSessionsQuery(Guid? modelId = null, bool onlyWithMeasurements = false, int? limit = null)
    {
        ModelId = modelId;
        OnlyWithMeasurements = onlyWithMeasurements;
        Limit = limit;
    }
}
