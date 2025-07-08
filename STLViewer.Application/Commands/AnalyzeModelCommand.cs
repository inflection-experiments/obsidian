using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to perform comprehensive analysis of an STL model.
/// </summary>
public sealed record AnalyzeModelCommand : IRequest<Result<MeasurementSession>>
{
    /// <summary>
    /// The unique identifier of the STL model to analyze.
    /// </summary>
    public Guid ModelId { get; init; }

    /// <summary>
    /// The unit of measurement to use for the analysis.
    /// </summary>
    public string Unit { get; init; } = "units";

    /// <summary>
    /// Whether to include detailed mesh statistics in the analysis.
    /// </summary>
    public bool IncludeStatistics { get; init; } = true;

    /// <summary>
    /// Creates a new model analysis command.
    /// </summary>
    /// <param name="modelId">The ID of the model to analyze.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="includeStatistics">Whether to include detailed statistics.</param>
    public AnalyzeModelCommand(Guid modelId, string unit = "units", bool includeStatistics = true)
    {
        ModelId = modelId;
        Unit = unit ?? "units";
        IncludeStatistics = includeStatistics;
    }
}
