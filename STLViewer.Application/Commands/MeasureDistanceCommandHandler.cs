using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands;

/// <summary>
/// Handler for measuring distance between two points.
/// </summary>
public sealed class MeasureDistanceCommandHandler : IRequestHandler<MeasureDistanceCommand, Result<DistanceMeasurement>>
{
    private readonly IMeasurementService _measurementService;

    /// <summary>
    /// Initializes a new instance of the MeasureDistanceCommandHandler class.
    /// </summary>
    /// <param name="measurementService">The measurement service.</param>
    public MeasureDistanceCommandHandler(IMeasurementService measurementService)
    {
        _measurementService = measurementService ?? throw new ArgumentNullException(nameof(measurementService));
    }

    /// <summary>
    /// Handles the distance measurement command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The distance measurement result.</returns>
    public async Task<Result<DistanceMeasurement>> Handle(MeasureDistanceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
                return Result<DistanceMeasurement>.Fail("Command cannot be null");

            var result = _measurementService.MeasureDistance(request.Point1, request.Point2, request.Unit);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Result<DistanceMeasurement>.Fail($"Failed to handle distance measurement command: {ex.Message}");
        }
    }
}
