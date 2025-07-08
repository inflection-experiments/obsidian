using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to measure the distance between two points in 3D space.
/// </summary>
public sealed record MeasureDistanceCommand : IRequest<Result<DistanceMeasurement>>
{
    /// <summary>
    /// The first point of the measurement.
    /// </summary>
    public Vector3 Point1 { get; init; }

    /// <summary>
    /// The second point of the measurement.
    /// </summary>
    public Vector3 Point2 { get; init; }

    /// <summary>
    /// The unit of measurement (e.g., "mm", "inches").
    /// </summary>
    public string Unit { get; init; } = "units";

    /// <summary>
    /// Creates a new distance measurement command.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <param name="unit">The unit of measurement.</param>
    public MeasureDistanceCommand(Vector3 point1, Vector3 point2, string unit = "units")
    {
        Point1 = point1;
        Point2 = point2;
        Unit = unit ?? "units";
    }
}
