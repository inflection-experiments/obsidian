using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands;

/// <summary>
/// Handler for measuring angle between three points.
/// </summary>
public sealed class MeasureAngleCommandHandler : IRequestHandler<MeasureAngleCommand, Result<AngleMeasurement>>
{
    private readonly IMeasurementService _measurementService;

    /// <summary>
    /// Initializes a new instance of the MeasureAngleCommandHandler class.
    /// </summary>
    /// <param name="measurementService">The measurement service.</param>
    public MeasureAngleCommandHandler(IMeasurementService measurementService)
    {
        _measurementService = measurementService ?? throw new ArgumentNullException(nameof(measurementService));
    }

    /// <summary>
    /// Handles the angle measurement command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The angle measurement result.</returns>
    public async Task<Result<AngleMeasurement>> Handle(MeasureAngleCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request == null)
                return Result<AngleMeasurement>.Fail("Command cannot be null");

            var result = _measurementService.MeasureAngle(request.Vertex, request.Point1, request.Point2);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Result<AngleMeasurement>.Fail($"Failed to handle angle measurement command: {ex.Message}");
        }
    }
}
