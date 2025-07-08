using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Application.DTOs;
using STLViewer.Application.Commands;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get available lighting presets.
/// </summary>
public class GetLightingPresetsQuery : IRequest<Result<IEnumerable<LightingPresetDto>>>
{
    /// <summary>
    /// Gets or sets whether to include predefined presets.
    /// </summary>
    public bool IncludePredefined { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include custom presets.
    /// </summary>
    public bool IncludeCustom { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include detailed light source information.
    /// </summary>
    public bool IncludeLightSources { get; set; } = false;
}

/// <summary>
/// DTO for lighting preset information.
/// </summary>
public class LightingPresetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public LightingPresetType Type { get; set; }
    public float GlobalAmbientIntensity { get; set; }
    public ColorDto GlobalAmbientColor { get; set; } = new();
    public int LightSourceCount { get; set; }
    public int EnabledLightCount { get; set; }
    public List<LightSourceDto>? LightSources { get; set; }
}

/// <summary>
/// DTO for light source information.
/// </summary>
public class LightSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LightType Type { get; set; }
    public bool IsEnabled { get; set; }
    public Vector3Dto? Position { get; set; }
    public Vector3Dto? Direction { get; set; }
    public ColorDto Color { get; set; } = new();
    public float Intensity { get; set; }
    public float? Range { get; set; }
    public float? InnerConeAngle { get; set; }
    public float? OuterConeAngle { get; set; }
    public float ConstantAttenuation { get; set; }
    public float LinearAttenuation { get; set; }
    public float QuadraticAttenuation { get; set; }
}

/// <summary>
/// DTO for color information.
/// </summary>
public class ColorDto
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; } = 1.0f;
}

/// <summary>
/// Handler for GetLightingPresetsQuery.
/// </summary>
public class GetLightingPresetsQueryHandler : IRequestHandler<GetLightingPresetsQuery, Result<IEnumerable<LightingPresetDto>>>
{
    public GetLightingPresetsQueryHandler()
    {
    }

    public Task<Result<IEnumerable<LightingPresetDto>>> Handle(GetLightingPresetsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var presets = new List<LightingPresetDto>();

            // Add predefined presets
            if (request.IncludePredefined)
            {
                var predefinedPresets = LightingPreset.GetPredefinedPresets();
                foreach (var preset in predefinedPresets)
                {
                    presets.Add(MapToDto(preset, request.IncludeLightSources));
                }
            }

            // Add custom presets (would normally come from a repository)
            if (request.IncludeCustom)
            {
                var currentPreset = SetLightingPresetCommandHandler.GetCurrentPreset();
                if (currentPreset != null && currentPreset.Type == LightingPresetType.Custom)
                {
                    presets.Add(MapToDto(currentPreset, request.IncludeLightSources));
                }
            }

            return Task.FromResult(Result<IEnumerable<LightingPresetDto>>.Ok(presets));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<LightingPresetDto>>.Fail($"Error getting lighting presets: {ex.Message}"));
        }
    }

    private static LightingPresetDto MapToDto(LightingPreset preset, bool includeLightSources)
    {
        var dto = new LightingPresetDto
        {
            Id = preset.Id,
            Name = preset.Name,
            Description = preset.Description,
            Type = preset.Type,
            GlobalAmbientIntensity = preset.GlobalAmbientIntensity,
            GlobalAmbientColor = new ColorDto
            {
                R = preset.GlobalAmbientColor.R,
                G = preset.GlobalAmbientColor.G,
                B = preset.GlobalAmbientColor.B,
                A = preset.GlobalAmbientColor.A
            },
            LightSourceCount = preset.LightSources.Count,
            EnabledLightCount = preset.EnabledLightCount
        };

        if (includeLightSources)
        {
            dto.LightSources = preset.LightSources.Select(MapLightSourceToDto).ToList();
        }

        return dto;
    }

    private static LightSourceDto MapLightSourceToDto(LightSource lightSource)
    {
        return new LightSourceDto
        {
            Id = lightSource.Id,
            Name = lightSource.Name,
            Type = lightSource.Type,
            IsEnabled = lightSource.IsEnabled,
            Position = lightSource.Position.HasValue ? new Vector3Dto
            {
                X = lightSource.Position.Value.X,
                Y = lightSource.Position.Value.Y,
                Z = lightSource.Position.Value.Z
            } : null,
            Direction = lightSource.Direction.HasValue ? new Vector3Dto
            {
                X = lightSource.Direction.Value.X,
                Y = lightSource.Direction.Value.Y,
                Z = lightSource.Direction.Value.Z
            } : null,
            Color = new ColorDto
            {
                R = lightSource.Color.R,
                G = lightSource.Color.G,
                B = lightSource.Color.B,
                A = lightSource.Color.A
            },
            Intensity = lightSource.Intensity,
            Range = lightSource.Range,
            InnerConeAngle = lightSource.InnerConeAngle,
            OuterConeAngle = lightSource.OuterConeAngle,
            ConstantAttenuation = lightSource.ConstantAttenuation,
            LinearAttenuation = lightSource.LinearAttenuation,
            QuadraticAttenuation = lightSource.QuadraticAttenuation
        };
    }
}
