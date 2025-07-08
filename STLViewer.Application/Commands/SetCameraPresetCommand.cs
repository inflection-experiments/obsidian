using FluentValidation;
using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Core.Interfaces;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to set a camera preset.
/// </summary>
public class SetCameraPresetCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the ID of the camera preset to apply.
    /// </summary>
    public string PresetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional bounding box to frame.
    /// </summary>
    public BoundingBox? BoundingBox { get; set; }

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
/// Validator for SetCameraPresetCommand.
/// </summary>
public class SetCameraPresetCommandValidator : AbstractValidator<SetCameraPresetCommand>
{
    public SetCameraPresetCommandValidator()
    {
        RuleFor(x => x.PresetId)
            .NotEmpty()
            .WithMessage("Preset ID is required");

        RuleFor(x => x.PresetId)
            .Must(BeValidPresetId)
            .WithMessage("Invalid preset ID. Valid presets: front, back, top, bottom, left, right, isometric, perspective");

        RuleFor(x => x.AnimationDurationMs)
            .InclusiveBetween(0, 5000)
            .WithMessage("Animation duration must be between 0 and 5000 milliseconds");
    }

    private bool BeValidPresetId(string presetId)
    {
        var validPresets = new[] { "front", "back", "top", "bottom", "left", "right", "isometric", "perspective" };
        return validPresets.Contains(presetId.ToLowerInvariant());
    }
}

/// <summary>
/// Handler for SetCameraPresetCommand.
/// </summary>
public class SetCameraPresetCommandHandler : IRequestHandler<SetCameraPresetCommand, Result>
{
    private readonly ICamera _camera;

    public SetCameraPresetCommandHandler(ICamera camera)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
    }

    public async Task<Result> Handle(SetCameraPresetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the camera preset
            var preset = GetCameraPreset(request.PresetId);

            // Adjust for bounding box if provided
            if (request.BoundingBox.HasValue)
            {
                preset = preset.AdjustForBoundingBox(request.BoundingBox.Value);
            }

            // Apply the camera preset
            if (request.Animated)
            {
                await AnimateCameraToPreset(preset, request.AnimationDurationMs, cancellationToken);
            }
            else
            {
                ApplyCameraPreset(preset);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to set camera preset '{request.PresetId}': {ex.Message}");
        }
    }

    private CameraPreset GetCameraPreset(string presetId)
    {
        return presetId.ToLowerInvariant() switch
        {
            "front" => CameraPreset.CreateFrontView(),
            "back" => CameraPreset.CreateBackView(),
            "top" => CameraPreset.CreateTopView(),
            "bottom" => CameraPreset.CreateBottomView(),
            "left" => CameraPreset.CreateLeftView(),
            "right" => CameraPreset.CreateRightView(),
            "isometric" => CameraPreset.CreateIsometricView(),
            "perspective" => CameraPreset.CreatePerspectiveView(),
            _ => throw new ArgumentException($"Unknown preset ID: {presetId}")
        };
    }

    private void ApplyCameraPreset(CameraPreset preset)
    {
        _camera.SetPosition(preset.Position);
        _camera.SetTarget(preset.Target);
        _camera.SetUp(preset.Up);
        _camera.SetFieldOfView(preset.FieldOfView * MathF.PI / 180.0f); // Convert degrees to radians
    }

    private async Task AnimateCameraToPreset(CameraPreset preset, int durationMs, CancellationToken cancellationToken)
    {
        // Get current camera state
        var startPosition = _camera.Position;
        var startTarget = _camera.Target;
        var startUp = _camera.Up;
        var startFov = _camera.FieldOfView * 180.0f / MathF.PI; // Convert radians to degrees

        // Target camera state
        var endPosition = preset.Position;
        var endTarget = preset.Target;
        var endUp = preset.Up;
        var endFov = preset.FieldOfView;

        // Animation parameters
        var startTime = DateTime.UtcNow;
        var duration = TimeSpan.FromMilliseconds(durationMs);

        while (DateTime.UtcNow - startTime < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate interpolation factor (0 to 1)
            var elapsed = DateTime.UtcNow - startTime;
            var t = MathF.Min(1.0f, (float)(elapsed.TotalMilliseconds / duration.TotalMilliseconds));

            // Apply easing function (smooth step)
            t = SmoothStep(t);

            // Interpolate camera parameters
            var currentPosition = Vector3.Lerp(startPosition, endPosition, t);
            var currentTarget = Vector3.Lerp(startTarget, endTarget, t);
            var currentUp = Vector3.Lerp(startUp, endUp, t);
            var currentFov = Lerp(startFov, endFov, t);

            // Apply interpolated values
            _camera.SetPosition(currentPosition);
            _camera.SetTarget(currentTarget);
            _camera.SetUp(currentUp.Normalized());
            _camera.SetFieldOfView(currentFov * MathF.PI / 180.0f); // Convert degrees to radians

            // Small delay to allow rendering
            await Task.Delay(16, cancellationToken); // ~60 FPS
        }

        // Ensure final position is exact
        ApplyCameraPreset(preset);
    }

    private float SmoothStep(float t)
    {
        // Smooth step function: 3t² - 2t³
        return t * t * (3.0f - 2.0f * t);
    }

    private float Lerp(float start, float end, float t)
    {
        return start + (end - start) * t;
    }
}
