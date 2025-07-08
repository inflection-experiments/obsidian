using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands;

/// <summary>
/// Handler for performing comprehensive model analysis.
/// </summary>
public sealed class AnalyzeModelCommandHandler : IRequestHandler<AnalyzeModelCommand, Result<MeasurementSession>>
{
    private readonly IMeasurementService _measurementService;
    private readonly IModelRepository _modelRepository;

    /// <summary>
    /// Initializes a new instance of the AnalyzeModelCommandHandler class.
    /// </summary>
    /// <param name="measurementService">The measurement service.</param>
    /// <param name="modelRepository">The model repository.</param>
    public AnalyzeModelCommandHandler(
        IMeasurementService measurementService,
        IModelRepository modelRepository)
    {
        _measurementService = measurementService ?? throw new ArgumentNullException(nameof(measurementService));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    /// <summary>
    /// Handles the model analysis command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The measurement session with analysis results.</returns>
    public async Task<Result<MeasurementSession>> Handle(AnalyzeModelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
                return Result<MeasurementSession>.Fail("Command cannot be null");

            // Retrieve the model from the repository
            var modelResult = await _modelRepository.GetByIdAsync(request.ModelId, cancellationToken);
            if (!modelResult.IsSuccess)
                return Result<MeasurementSession>.Fail($"Failed to retrieve model: {modelResult.Error}");

            var model = modelResult.Value;
            if (model == null)
                return Result<MeasurementSession>.Fail($"Model with ID {request.ModelId} not found");

            // Perform comprehensive analysis using the measurement service
            var analysisResult = await _measurementService.PerformComprehensiveAnalysisAsync(
                model,
                request.Unit,
                cancellationToken);

            if (!analysisResult.IsSuccess)
                return Result<MeasurementSession>.Fail($"Failed to perform analysis: {analysisResult.Error}");

            var session = analysisResult.Value;

            // If requested, add mesh statistics as additional information
            if (request.IncludeStatistics)
            {
                var statisticsResult = await _measurementService.CalculateMeshStatisticsAsync(model, cancellationToken);
                if (statisticsResult.IsSuccess)
                {
                    // The statistics are returned as a dictionary, which could be logged or stored
                    // For now, we'll just ensure the analysis completed successfully
                    var stats = statisticsResult.Value;
                    var meshQuality = stats.TryGetValue("MeshQualityScore", out var quality) ? quality : "Unknown";

                    // Update session name to include quality indicator
                    session = session.Rename($"{session.Name} (Quality: {meshQuality:F2})");
                }
            }

            return Result.Ok(session);
        }
        catch (OperationCanceledException)
        {
            return Result<MeasurementSession>.Fail("Model analysis was cancelled");
        }
        catch (Exception ex)
        {
            return Result<MeasurementSession>.Fail($"Failed to handle model analysis command: {ex.Message}");
        }
    }
}
