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
    private readonly ICameraAnimationService _animationService;

    public SetCameraPresetCommandHandler(ICamera camera, ICameraAnimationService animationService)
    {
        _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
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
                var animationResult = await _animationService.AnimateToCameraPresetAsync(
                    _camera, preset, request.AnimationDurationMs, cancellationToken);

                if (animationResult.IsFailure)
                {
                    return Result.Fail($"Animation failed: {animationResult.Error}");
                }
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


}
