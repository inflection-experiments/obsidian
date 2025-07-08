using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Infrastructure.Services;

/// <summary>
/// Service for managing lighting in the 3D scene.
/// </summary>
public class LightingService : ILightingService
{
    private LightingPreset? _currentPreset;
    private bool _isLightingEnabled = true;

    /// <inheritdoc/>
    public LightingPreset? CurrentPreset => _currentPreset;

    /// <inheritdoc/>
    public bool IsLightingEnabled => _isLightingEnabled;

    /// <inheritdoc/>
    public event EventHandler<LightingPresetChangedEventArgs>? LightingPresetChanged;

    /// <inheritdoc/>
    public event EventHandler<LightSourceUpdatedEventArgs>? LightSourceUpdated;

    /// <summary>
    /// Initializes a new instance of the LightingService class.
    /// </summary>
    public LightingService()
    {
        // Set default lighting preset
        _currentPreset = LightingPreset.CreateBasic();
    }

    /// <inheritdoc/>
    public Task<Result> SetLightingPresetAsync(LightingPreset preset, bool animated = true, int durationMs = 1000)
    {
        try
        {
            if (preset == null)
                return Task.FromResult(Result.Fail("Lighting preset cannot be null"));

            var oldPreset = _currentPreset;
            _currentPreset = preset;

            // Raise event
            LightingPresetChanged?.Invoke(this, new LightingPresetChangedEventArgs(oldPreset, preset, animated));

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error setting lighting preset: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public Task<Result> UpdateLightSourceAsync(Guid lightId, LightSource updatedLight)
    {
        try
        {
            if (_currentPreset == null)
                return Task.FromResult(Result.Fail("No lighting preset is currently active"));

            var existingLight = _currentPreset.LightSources.FirstOrDefault(l => l.Id == lightId);
            if (existingLight == null)
                return Task.FromResult(Result.Fail($"Light source with ID {lightId} not found"));

            // Update the light source in the current preset
            var updatedLightSources = _currentPreset.LightSources
                .Select(l => l.Id == lightId ? updatedLight : l)
                .ToList();

            var updatedPreset = _currentPreset.WithLightSources(updatedLightSources);

            var oldPreset = _currentPreset;
            _currentPreset = updatedPreset;

            // Raise events
            LightSourceUpdated?.Invoke(this, new LightSourceUpdatedEventArgs(lightId, existingLight, updatedLight));
            LightingPresetChanged?.Invoke(this, new LightingPresetChangedEventArgs(oldPreset, updatedPreset, false));

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error updating light source: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public Task<Result> SetLightingEnabledAsync(bool enabled)
    {
        try
        {
            _isLightingEnabled = enabled;
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error setting lighting enabled state: {ex.Message}"));
        }
    }

    /// <inheritdoc/>
    public IEnumerable<LightingPreset> GetPredefinedPresets()
    {
        return LightingPreset.GetPredefinedPresets();
    }

    /// <inheritdoc/>
    public Result<LightingPreset> CreateCustomPreset(
        string name,
        string description,
        IEnumerable<LightSource> lightSources,
        float globalAmbientIntensity = 0.1f,
        Color? globalAmbientColor = null)
    {
        try
        {
            var preset = LightingPreset.CreateCustom(
                name,
                description,
                lightSources,
                globalAmbientIntensity,
                globalAmbientColor);

            return Result<LightingPreset>.Ok(preset);
        }
        catch (Exception ex)
        {
            return Result<LightingPreset>.Fail($"Error creating custom preset: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void ApplyLightingToRenderSettings(RenderSettings renderSettings)
    {
        if (renderSettings == null)
            return;

        renderSettings.Lighting.Enabled = _isLightingEnabled && _currentPreset != null;

        if (!renderSettings.Lighting.Enabled || _currentPreset == null)
        {
            return;
        }

        // Apply global ambient settings
        renderSettings.Lighting.AmbientColor = new Color(
            _currentPreset.GlobalAmbientColor.R * _currentPreset.GlobalAmbientIntensity,
            _currentPreset.GlobalAmbientColor.G * _currentPreset.GlobalAmbientIntensity,
            _currentPreset.GlobalAmbientColor.B * _currentPreset.GlobalAmbientIntensity,
            _currentPreset.GlobalAmbientColor.A);

        // Find the primary directional light for main lighting
        var primaryDirectional = _currentPreset.LightSources
            .Where(l => l.IsEnabled && l.Type == LightType.Directional)
            .OrderByDescending(l => l.Intensity)
            .FirstOrDefault();

        if (primaryDirectional != null)
        {
            renderSettings.Lighting.LightDirection = primaryDirectional.Direction ?? new Vector3(-1, -1, -1);
            renderSettings.Lighting.DiffuseColor = primaryDirectional.EffectiveColor;
            renderSettings.Lighting.SpecularColor = primaryDirectional.EffectiveColor;
        }
        else
        {
            // Fallback to default directional lighting
            renderSettings.Lighting.LightDirection = new Vector3(-0.5f, -1.0f, -0.5f);
            renderSettings.Lighting.DiffuseColor = Color.White;
            renderSettings.Lighting.SpecularColor = Color.White;
        }

        // Set shininess from material (this should be coordinated with material settings)
        renderSettings.Lighting.Shininess = 32.0f;
    }

    /// <summary>
    /// Gets lighting statistics for debugging and monitoring.
    /// </summary>
    public LightingStatistics GetLightingStatistics()
    {
        if (_currentPreset == null)
        {
            return new LightingStatistics
            {
                PresetName = "None",
                TotalLights = 0,
                EnabledLights = 0,
                AmbientLights = 0,
                DirectionalLights = 0,
                PointLights = 0,
                SpotLights = 0,
                GlobalAmbientIntensity = 0,
                AverageIntensity = 0
            };
        }

        var lights = _currentPreset.LightSources;
        var enabledLights = lights.Where(l => l.IsEnabled).ToList();

        return new LightingStatistics
        {
            PresetName = _currentPreset.Name,
            TotalLights = lights.Count,
            EnabledLights = enabledLights.Count,
            AmbientLights = lights.Count(l => l.Type == LightType.Ambient),
            DirectionalLights = lights.Count(l => l.Type == LightType.Directional),
            PointLights = lights.Count(l => l.Type == LightType.Point),
            SpotLights = lights.Count(l => l.Type == LightType.Spot),
            GlobalAmbientIntensity = _currentPreset.GlobalAmbientIntensity,
            AverageIntensity = enabledLights.Any() ? enabledLights.Average(l => l.Intensity) : 0
        };
    }
}

/// <summary>
/// Statistics about the current lighting setup.
/// </summary>
public class LightingStatistics
{
    public string PresetName { get; set; } = string.Empty;
    public int TotalLights { get; set; }
    public int EnabledLights { get; set; }
    public int AmbientLights { get; set; }
    public int DirectionalLights { get; set; }
    public int PointLights { get; set; }
    public int SpotLights { get; set; }
    public float GlobalAmbientIntensity { get; set; }
    public float AverageIntensity { get; set; }

    public override string ToString()
    {
        return $"Preset: {PresetName}, Lights: {EnabledLights}/{TotalLights}, " +
               $"Ambient: {GlobalAmbientIntensity:F2}, Avg Intensity: {AverageIntensity:F2}";
    }
}
