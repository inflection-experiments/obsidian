using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to reset the camera to its default position.
/// </summary>
public class ResetCameraCommand : IRequest<Result>
{
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
/// Handler for ResetCameraCommand.
/// </summary>
public class ResetCameraCommandHandler : IRequestHandler<ResetCameraCommand, Result>
{
    private readonly ICamera _camera;
    private readonly ICameraAnimationService _animationService;

    public ResetCameraCommandHandler(ICamera camera, ICameraAnimationService animationService)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
    }

    public async Task<Result> Handle(ResetCameraCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Create default camera preset
            var defaultPreset = CameraPreset.CreatePerspectiveView();

            // Apply the camera preset
            if (request.Animated)
            {
                var animationResult = await _animationService.AnimateToCameraPresetAsync(
                    _camera, defaultPreset, request.AnimationDurationMs, cancellationToken);

                if (animationResult.IsFailure)
                {
                    return Result.Fail($"Animation failed: {animationResult.Error}");
                }
            }
            else
            {
                _camera.SetPosition(defaultPreset.Position);
                _camera.SetTarget(defaultPreset.Target);
                _camera.SetUp(defaultPreset.Up);
                _camera.SetFieldOfView(defaultPreset.FieldOfView * MathF.PI / 180.0f); // Convert degrees to radians
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to reset camera: {ex.Message}");
        }
    }
}
