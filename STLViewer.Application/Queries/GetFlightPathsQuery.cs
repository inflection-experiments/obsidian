using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;
using STLViewer.Application.DTOs;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get flight paths from the flight path plugin.
/// </summary>
public class GetFlightPathsQuery : IRequest<List<FlightPathDto>>
{
    /// <summary>
    /// Gets or sets the name of a specific flight path to retrieve (optional).
    /// </summary>
    public string? FlightPathName { get; set; }

    /// <summary>
    /// Gets or sets whether to include trajectory data.
    /// </summary>
    public bool IncludeTrajectory { get; set; } = false;

    /// <summary>
    /// Gets or sets the resolution for trajectory calculation (default: 100).
    /// </summary>
    public int TrajectoryResolution { get; set; } = 100;
}

/// <summary>
/// DTO for flight path information.
/// </summary>
public class FlightPathDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public TrajectoryInterpolation InterpolationMethod { get; set; }
    public float TotalLength { get; set; }
    public float EstimatedFlightTime { get; set; }
    public int WaypointCount { get; set; }
    public List<WaypointInfoDto> Waypoints { get; set; } = new();
    public Vector3[]? Trajectory { get; set; }
    public BoundingBoxDto BoundingBox { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int LoopCount { get; set; }
    public string EasingType { get; set; } = string.Empty;
    public float Duration { get; set; }
}

/// <summary>
/// DTO for waypoint information.
/// </summary>
public class WaypointInfoDto
{
    public Guid Id { get; set; }
    public Vector3 Position { get; set; }
    public string? Name { get; set; }
    public float? Altitude { get; set; }
    public float? Speed { get; set; }
    public float? Heading { get; set; }
    public float? Pitch { get; set; }
    public float? Roll { get; set; }
    public float? DwellTime { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Handler for GetFlightPathsQuery.
/// </summary>
public class GetFlightPathsQueryHandler : IRequestHandler<GetFlightPathsQuery, List<FlightPathDto>>
{
    public GetFlightPathsQueryHandler()
    {
    }

    public Task<List<FlightPathDto>> Handle(GetFlightPathsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // This query will be handled by the UI layer which has access to the plugin manager
            // For now, return an empty collection as a placeholder
            var emptyResult = new List<FlightPathDto>();
            return Task.FromResult(emptyResult);
        }
        catch (Exception)
        {
            // In case of error, return empty list rather than throwing
            return Task.FromResult(new List<FlightPathDto>());
        }
    }
}
