using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to measure the angle between three points in 3D space.
/// </summary>
public sealed record MeasureAngleCommand : IRequest<Result<AngleMeasurement>>
{
    /// <summary>
    /// The vertex point where the angle is measured.
    /// </summary>
    public Vector3 Vertex { get; init; }

    /// <summary>
    /// The first point defining one ray of the angle.
    /// </summary>
    public Vector3 Point1 { get; init; }

    /// <summary>
    /// The second point defining the other ray of the angle.
    /// </summary>
    public Vector3 Point2 { get; init; }

    /// <summary>
    /// Creates a new angle measurement command.
    /// </summary>
    /// <param name="vertex">The vertex point of the angle.</param>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    public MeasureAngleCommand(Vector3 vertex, Vector3 point1, Vector3 point2)
    {
        Vertex = vertex;
        Point1 = point1;
        Point2 = point2;
    }
}
