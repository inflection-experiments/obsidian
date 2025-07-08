using FluentValidation;
using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to set the current lighting preset.
/// </summary>
public class SetLightingPresetCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the type of lighting preset to apply.
    /// </summary>
    public LightingPresetType PresetType { get; set; } = LightingPresetType.Basic;

    /// <summary>
    /// Gets or sets whether to animate the lighting transition.
    /// </summary>
    public bool Animated { get; set; } = true;

    /// <summary>
    /// Gets or sets the animation duration in milliseconds.
    /// </summary>
    public int AnimationDurationMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the custom lighting preset (if PresetType is Custom).
    /// </summary>
    public LightingPreset? CustomPreset { get; set; }
}

/// <summary>
/// Validator for SetLightingPresetCommand.
/// </summary>
public class SetLightingPresetCommandValidator : AbstractValidator<SetLightingPresetCommand>
{
    public SetLightingPresetCommandValidator()
    {
        RuleFor(x => x.PresetType)
            .IsInEnum()
            .WithMessage("Invalid lighting preset type");

        RuleFor(x => x.AnimationDurationMs)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Animation duration must be non-negative")
            .LessThanOrEqualTo(5000)
            .WithMessage("Animation duration cannot exceed 5 seconds");

        RuleFor(x => x.CustomPreset)
            .NotNull()
            .When(x => x.PresetType == LightingPresetType.Custom)
            .WithMessage("Custom preset is required when preset type is Custom");

        RuleFor(x => x.CustomPreset)
            .Null()
            .When(x => x.PresetType != LightingPresetType.Custom)
            .WithMessage("Custom preset should be null for predefined preset types");
    }
}

/// <summary>
/// Handler for SetLightingPresetCommand.
/// </summary>
public class SetLightingPresetCommandHandler : IRequestHandler<SetLightingPresetCommand, Result>
{
    private static LightingPreset? _currentPreset;

    public SetLightingPresetCommandHandler()
    {
    }

    public Task<Result> Handle(SetLightingPresetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the lighting preset to apply
            LightingPreset preset;
            if (request.PresetType == LightingPresetType.Custom && request.CustomPreset != null)
            {
                preset = request.CustomPreset;
            }
            else
            {
                preset = GetPredefinedPreset(request.PresetType);
            }

            // Apply the lighting preset
            if (request.Animated)
            {
                // In a real implementation, this would trigger a smooth transition
                // For now, we'll just apply it immediately
                _currentPreset = preset;
            }
            else
            {
                _currentPreset = preset;
            }

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error setting lighting preset: {ex.Message}"));
        }
    }

    private static LightingPreset GetPredefinedPreset(LightingPresetType type)
    {
        return type switch
        {
            LightingPresetType.Basic => LightingPreset.CreateBasic(),
            LightingPresetType.Studio => LightingPreset.CreateStudio(),
            LightingPresetType.Outdoor => LightingPreset.CreateOutdoor(),
            LightingPresetType.Indoor => LightingPreset.CreateIndoor(),
            LightingPresetType.Technical => LightingPreset.CreateTechnical(),
            LightingPresetType.Dramatic => LightingPreset.CreateDramatic(),
            LightingPresetType.Showcase => LightingPreset.CreateShowcase(),
            _ => LightingPreset.CreateBasic()
        };
    }

    /// <summary>
    /// Gets the current lighting preset (for testing purposes).
    /// </summary>
    public static LightingPreset? GetCurrentPreset() => _currentPreset;
}
