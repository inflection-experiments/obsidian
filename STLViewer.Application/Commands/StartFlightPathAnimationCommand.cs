using FluentValidation;
using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to start flight path animation.
/// </summary>
public class StartFlightPathAnimationCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the name of the flight path to animate.
    /// </summary>
    public string FlightPathName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the animation speed (default: 1.0).
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether to reset the animation to the beginning.
    /// </summary>
    public bool ResetToBeginning { get; set; } = true;
}

/// <summary>
/// Validator for StartFlightPathAnimationCommand.
/// </summary>
public class StartFlightPathAnimationCommandValidator : AbstractValidator<StartFlightPathAnimationCommand>
{
    public StartFlightPathAnimationCommandValidator()
    {
        RuleFor(x => x.FlightPathName)
            .NotEmpty()
            .WithMessage("Flight path name is required");

        RuleFor(x => x.Speed)
            .GreaterThan(0)
            .WithMessage("Animation speed must be greater than 0")
            .LessThanOrEqualTo(10)
            .WithMessage("Animation speed cannot exceed 10");
    }
}

/// <summary>
/// Handler for StartFlightPathAnimationCommand.
/// </summary>
public class StartFlightPathAnimationCommandHandler : IRequestHandler<StartFlightPathAnimationCommand, Result>
{
    public StartFlightPathAnimationCommandHandler()
    {
    }

    public Task<Result> Handle(StartFlightPathAnimationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // This command will be handled by the UI layer which has access to the plugin manager
            // For now, this is a placeholder that indicates the command was processed successfully
            // The actual animation logic will be handled at the UI level

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error processing flight path animation command: {ex.Message}"));
        }
    }
}
