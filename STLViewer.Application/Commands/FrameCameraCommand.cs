using FluentValidation;
using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to frame the camera to fit a bounding box.
/// </summary>
public class FrameCameraCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the bounding box to frame.
    /// </summary>
    public BoundingBox BoundingBox { get; set; }

    /// <summary>
    /// Gets or sets the padding factor (default 1.2 for 20% padding).
    /// </summary>
    public float PaddingFactor { get; set; } = 1.2f;

    /// <summary>
    /// Gets or sets whether to animate the camera transition.
    /// </summary>
    public bool Animated { get; set; } = true;

    /// <summary>
    /// Gets or sets the animation duration in milliseconds.
    /// </summary>
    public int AnimationDurationMs { get; set; } = 500;
}

/// <summary>
/// Validator for FrameCameraCommand.
/// </summary>
public class FrameCameraCommandValidator : AbstractValidator<FrameCameraCommand>
{
    public FrameCameraCommandValidator()
    {
        RuleFor(x => x.PaddingFactor)
            .GreaterThan(0.1f)
            .WithMessage("Padding factor must be greater than 0.1");

        RuleFor(x => x.PaddingFactor)
            .LessThan(5.0f)
            .WithMessage("Padding factor must be less than 5.0");

        RuleFor(x => x.AnimationDurationMs)
            .InclusiveBetween(0, 5000)
            .WithMessage("Animation duration must be between 0 and 5000 milliseconds");
    }
}

/// <summary>
/// Handler for FrameCameraCommand.
/// </summary>
public class FrameCameraCommandHandler : IRequestHandler<FrameCameraCommand, Result>
{
    private readonly ICamera _camera;
    private readonly ICameraAnimationService _animationService;

    public FrameCameraCommandHandler(ICamera camera, ICameraAnimationService animationService)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
    }

    public async Task<Result> Handle(FrameCameraCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Calculate camera position to frame the bounding box
            var center = request.BoundingBox.Center;
            var size = request.BoundingBox.Size;
            var maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));

            if (maxDimension < float.Epsilon)
            {
                return Result.Fail("Bounding box is too small to frame");
            }

            // Calculate distance based on field of view and padding
            var fov = _camera.FieldOfView;
            var distance = (maxDimension * request.PaddingFactor) / (2.0f * MathF.Tan(fov * 0.5f));

            // Use current camera direction or default to perspective view
            var currentDirection = (_camera.Position - _camera.Target).Normalized();
            if (currentDirection.LengthSquared < float.Epsilon)
            {
                currentDirection = new Vector3(0.5f, 0.3f, 0.8f).Normalized();
            }

            // Create camera preset for framing
            var framePreset = new CameraPreset(
                "frame",
                "Frame View",
                center + currentDirection * distance,
                center,
                Vector3.UnitY,
                fov * 180.0f / MathF.PI, // Convert radians to degrees
                "Framed view of the model"
            );

            // Apply the camera preset
            if (request.Animated)
            {
                var animationResult = await _animationService.AnimateToCameraPresetAsync(
                    _camera, framePreset, request.AnimationDurationMs, cancellationToken);

                if (animationResult.IsFailure)
                {
                    return Result.Fail($"Animation failed: {animationResult.Error}");
                }
            }
            else
            {
                _camera.SetPosition(framePreset.Position);
                _camera.SetTarget(framePreset.Target);
                _camera.SetUp(framePreset.Up);
                _camera.SetFieldOfView(framePreset.FieldOfView * MathF.PI / 180.0f); // Convert degrees to radians
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to frame camera: {ex.Message}");
        }
    }
}
