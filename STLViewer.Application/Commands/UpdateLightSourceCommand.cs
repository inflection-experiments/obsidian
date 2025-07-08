using FluentValidation;
using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to update an individual light source.
/// </summary>
public class UpdateLightSourceCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the ID of the light source to update.
    /// </summary>
    public Guid LightId { get; set; }

    /// <summary>
    /// Gets or sets whether the light is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the light intensity (0.0 to 1.0).
    /// </summary>
    public float? Intensity { get; set; }

    /// <summary>
    /// Gets or sets the light color.
    /// </summary>
    public Color? Color { get; set; }

    /// <summary>
    /// Gets or sets the light position (for Point and Spot lights).
    /// </summary>
    public Vector3? Position { get; set; }

    /// <summary>
    /// Gets or sets the light direction (for Directional and Spot lights).
    /// </summary>
    public Vector3? Direction { get; set; }

    /// <summary>
    /// Gets or sets the light range (for Point and Spot lights).
    /// </summary>
    public float? Range { get; set; }

    /// <summary>
    /// Gets or sets the constant attenuation factor.
    /// </summary>
    public float? ConstantAttenuation { get; set; }

    /// <summary>
    /// Gets or sets the linear attenuation factor.
    /// </summary>
    public float? LinearAttenuation { get; set; }

    /// <summary>
    /// Gets or sets the quadratic attenuation factor.
    /// </summary>
    public float? QuadraticAttenuation { get; set; }

    /// <summary>
    /// Gets or sets the inner cone angle for spot lights (in radians).
    /// </summary>
    public float? InnerConeAngle { get; set; }

    /// <summary>
    /// Gets or sets the outer cone angle for spot lights (in radians).
    /// </summary>
    public float? OuterConeAngle { get; set; }
}

/// <summary>
/// Validator for UpdateLightSourceCommand.
/// </summary>
public class UpdateLightSourceCommandValidator : AbstractValidator<UpdateLightSourceCommand>
{
    public UpdateLightSourceCommandValidator()
    {
        RuleFor(x => x.LightId)
            .NotEmpty()
            .WithMessage("Light ID is required");

        RuleFor(x => x.Intensity)
            .InclusiveBetween(0.0f, 1.0f)
            .When(x => x.Intensity.HasValue)
            .WithMessage("Intensity must be between 0.0 and 1.0");

        RuleFor(x => x.Range)
            .GreaterThan(0)
            .When(x => x.Range.HasValue)
            .WithMessage("Range must be greater than 0");

        RuleFor(x => x.ConstantAttenuation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ConstantAttenuation.HasValue)
            .WithMessage("Constant attenuation must be non-negative");

        RuleFor(x => x.LinearAttenuation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LinearAttenuation.HasValue)
            .WithMessage("Linear attenuation must be non-negative");

        RuleFor(x => x.QuadraticAttenuation)
            .GreaterThanOrEqualTo(0)
            .When(x => x.QuadraticAttenuation.HasValue)
            .WithMessage("Quadratic attenuation must be non-negative");

        RuleFor(x => x.InnerConeAngle)
            .GreaterThan(0)
            .LessThan(MathF.PI)
            .When(x => x.InnerConeAngle.HasValue)
            .WithMessage("Inner cone angle must be between 0 and PI radians");

        RuleFor(x => x.OuterConeAngle)
            .GreaterThan(0)
            .LessThan(MathF.PI)
            .When(x => x.OuterConeAngle.HasValue)
            .WithMessage("Outer cone angle must be between 0 and PI radians");

        RuleFor(x => x)
            .Must(x => !x.OuterConeAngle.HasValue || !x.InnerConeAngle.HasValue || x.OuterConeAngle > x.InnerConeAngle)
            .WithMessage("Outer cone angle must be greater than inner cone angle");
    }
}

/// <summary>
/// Handler for UpdateLightSourceCommand.
/// </summary>
public class UpdateLightSourceCommandHandler : IRequestHandler<UpdateLightSourceCommand, Result>
{
    public UpdateLightSourceCommandHandler()
    {
    }

    public Task<Result> Handle(UpdateLightSourceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get the current lighting preset
            var currentPreset = SetLightingPresetCommandHandler.GetCurrentPreset();
            if (currentPreset == null)
            {
                return Task.FromResult(Result.Fail("No lighting preset is currently active"));
            }

            // Find the light source to update
            var existingLight = currentPreset.LightSources.FirstOrDefault(l => l.Id == request.LightId);
            if (existingLight == null)
            {
                return Task.FromResult(Result.Fail($"Light source with ID {request.LightId} not found"));
            }

            // Create updated light source
            var updatedLight = existingLight;

            if (request.IsEnabled.HasValue)
            {
                updatedLight = updatedLight.WithEnabled(request.IsEnabled.Value);
            }

            if (request.Intensity.HasValue)
            {
                updatedLight = updatedLight.WithIntensity(request.Intensity.Value);
            }

            if (request.Color.HasValue)
            {
                updatedLight = updatedLight.WithColor(request.Color.Value);
            }

            if (request.Position.HasValue)
            {
                if (existingLight.Type == LightType.Point || existingLight.Type == LightType.Spot)
                {
                    updatedLight = updatedLight.WithPosition(request.Position.Value);
                }
                else
                {
                    return Task.FromResult(Result.Fail($"Cannot set position on {existingLight.Type} light"));
                }
            }

            if (request.Direction.HasValue)
            {
                if (existingLight.Type == LightType.Directional || existingLight.Type == LightType.Spot)
                {
                    updatedLight = updatedLight.WithDirection(request.Direction.Value);
                }
                else
                {
                    return Task.FromResult(Result.Fail($"Cannot set direction on {existingLight.Type} light"));
                }
            }

            // Update the light sources in the preset
            var updatedLightSources = currentPreset.LightSources
                .Select(l => l.Id == request.LightId ? updatedLight : l)
                .ToList();

            var updatedPreset = currentPreset.WithLightSources(updatedLightSources);

            // Apply the updated preset (this would normally go through the SetLightingPresetCommand)
            var setPresetCommand = new SetLightingPresetCommand
            {
                PresetType = LightingPresetType.Custom,
                CustomPreset = updatedPreset,
                Animated = false
            };

            // For now, we'll just update the static reference
            // In a real implementation, this would use proper state management
            var handler = new SetLightingPresetCommandHandler();
            return handler.Handle(setPresetCommand, cancellationToken);
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error updating light source: {ex.Message}"));
        }
    }
}
