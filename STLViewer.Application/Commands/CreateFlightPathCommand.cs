using FluentValidation;
using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to create a new flight path.
/// </summary>
public class CreateFlightPathCommand : IRequest<Result<FlightPath>>
{
    /// <summary>
    /// Gets or sets the name of the flight path.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the flight path.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the interpolation method for the flight path.
    /// </summary>
    public TrajectoryInterpolation InterpolationMethod { get; set; } = TrajectoryInterpolation.Linear;

    /// <summary>
    /// Gets or sets whether the flight path should be closed.
    /// </summary>
    public bool IsClosed { get; set; } = false;

    /// <summary>
    /// Gets or sets the initial waypoints for the flight path.
    /// </summary>
    public List<WaypointDto> Waypoints { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to create a sample flight path if no waypoints are provided.
    /// </summary>
    public bool CreateSamplePath { get; set; } = false;
}

/// <summary>
/// DTO for waypoint data in commands.
/// </summary>
public class WaypointDto
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public string? Name { get; set; }
    public float? Altitude { get; set; }
    public float? Speed { get; set; }
    public float? Heading { get; set; }
    public float? Pitch { get; set; }
    public float? Roll { get; set; }
    public float? DwellTime { get; set; }
}

/// <summary>
/// Validator for CreateFlightPathCommand.
/// </summary>
public class CreateFlightPathCommandValidator : AbstractValidator<CreateFlightPathCommand>
{
    public CreateFlightPathCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Flight path name is required")
            .MaximumLength(100)
            .WithMessage("Flight path name cannot exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.InterpolationMethod)
            .IsInEnum()
            .WithMessage("Invalid interpolation method");

        RuleForEach(x => x.Waypoints)
            .SetValidator(new WaypointDtoValidator());

        RuleFor(x => x.Waypoints)
            .Must(waypoints => waypoints.Count >= 2 || waypoints.Count == 0)
            .WithMessage("Flight path must have at least 2 waypoints (or none for sample path)");
    }
}

/// <summary>
/// Validator for WaypointDto.
/// </summary>
public class WaypointDtoValidator : AbstractValidator<WaypointDto>
{
    public WaypointDtoValidator()
    {
        RuleFor(x => x.X)
            .Must(BeFinite)
            .WithMessage("X coordinate must be a finite number");

        RuleFor(x => x.Y)
            .Must(BeFinite)
            .WithMessage("Y coordinate must be a finite number");

        RuleFor(x => x.Z)
            .Must(BeFinite)
            .WithMessage("Z coordinate must be a finite number");

        RuleFor(x => x.Name)
            .MaximumLength(50)
            .WithMessage("Waypoint name cannot exceed 50 characters");

        RuleFor(x => x.Speed)
            .GreaterThan(0)
            .When(x => x.Speed.HasValue)
            .WithMessage("Speed must be greater than 0");

        RuleFor(x => x.Heading)
            .InclusiveBetween(-180, 180)
            .When(x => x.Heading.HasValue)
            .WithMessage("Heading must be between -180 and 180 degrees");

        RuleFor(x => x.Pitch)
            .InclusiveBetween(-90, 90)
            .When(x => x.Pitch.HasValue)
            .WithMessage("Pitch must be between -90 and 90 degrees");

        RuleFor(x => x.Roll)
            .InclusiveBetween(-180, 180)
            .When(x => x.Roll.HasValue)
            .WithMessage("Roll must be between -180 and 180 degrees");

        RuleFor(x => x.DwellTime)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DwellTime.HasValue)
            .WithMessage("Dwell time must be non-negative");
    }

    private bool BeFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// Handler for CreateFlightPathCommand.
/// </summary>
public class CreateFlightPathCommandHandler : IRequestHandler<CreateFlightPathCommand, Result<FlightPath>>
{
    public CreateFlightPathCommandHandler()
    {
    }

    public Task<Result<FlightPath>> Handle(CreateFlightPathCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // This command will be handled by the UI layer which has access to the plugin manager
            // For now, create a basic FlightPath instance as placeholder
            var flightPath = FlightPath.Create(request.Name, request.Description);
            flightPath.SetInterpolationMethod(request.InterpolationMethod);
            flightPath.SetClosed(request.IsClosed);

            return Task.FromResult(Result<FlightPath>.Ok(flightPath));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<FlightPath>.Fail($"Error creating flight path: {ex.Message}"));
        }
    }
}
