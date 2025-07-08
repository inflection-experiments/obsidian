using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Service for managing lighting in the 3D scene.
/// </summary>
public interface ILightingService
{
    /// <summary>
    /// Gets the currently active lighting preset.
    /// </summary>
    LightingPreset? CurrentPreset { get; }

    /// <summary>
    /// Gets a value indicating whether lighting is enabled globally.
    /// </summary>
    bool IsLightingEnabled { get; }

    /// <summary>
    /// Event raised when the lighting preset changes.
    /// </summary>
    event EventHandler<LightingPresetChangedEventArgs>? LightingPresetChanged;

    /// <summary>
    /// Event raised when a light source is updated.
    /// </summary>
    event EventHandler<LightSourceUpdatedEventArgs>? LightSourceUpdated;

    /// <summary>
    /// Sets the lighting preset.
    /// </summary>
    /// <param name="preset">The lighting preset to apply.</param>
    /// <param name="animated">Whether to animate the transition.</param>
    /// <param name="durationMs">The animation duration in milliseconds.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetLightingPresetAsync(LightingPreset preset, bool animated = true, int durationMs = 1000);

    /// <summary>
    /// Updates a specific light source.
    /// </summary>
    /// <param name="lightId">The ID of the light to update.</param>
    /// <param name="updatedLight">The updated light source.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateLightSourceAsync(Guid lightId, LightSource updatedLight);

    /// <summary>
    /// Enables or disables global lighting.
    /// </summary>
    /// <param name="enabled">Whether lighting should be enabled.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> SetLightingEnabledAsync(bool enabled);

    /// <summary>
    /// Gets all available predefined lighting presets.
    /// </summary>
    /// <returns>The available predefined lighting presets.</returns>
    IEnumerable<LightingPreset> GetPredefinedPresets();

    /// <summary>
    /// Creates a custom lighting preset.
    /// </summary>
    /// <param name="name">The name of the preset.</param>
    /// <param name="description">The description of the preset.</param>
    /// <param name="lightSources">The light sources in the preset.</param>
    /// <param name="globalAmbientIntensity">The global ambient intensity.</param>
    /// <param name="globalAmbientColor">The global ambient color.</param>
    /// <returns>A result containing the created preset or an error.</returns>
    Result<LightingPreset> CreateCustomPreset(
        string name,
        string description,
        IEnumerable<LightSource> lightSources,
        float globalAmbientIntensity = 0.1f,
        Color? globalAmbientColor = null);

    /// <summary>
    /// Applies the current lighting to the render settings.
    /// </summary>
    /// <param name="renderSettings">The render settings to update.</param>
    void ApplyLightingToRenderSettings(RenderSettings renderSettings);
}

/// <summary>
/// Event arguments for lighting preset changed events.
/// </summary>
public class LightingPresetChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the old lighting preset.
    /// </summary>
    public LightingPreset? OldPreset { get; }

    /// <summary>
    /// Gets the new lighting preset.
    /// </summary>
    public LightingPreset NewPreset { get; }

    /// <summary>
    /// Gets whether the change was animated.
    /// </summary>
    public bool Animated { get; }

    /// <summary>
    /// Initializes a new instance of the LightingPresetChangedEventArgs class.
    /// </summary>
    public LightingPresetChangedEventArgs(LightingPreset? oldPreset, LightingPreset newPreset, bool animated)
    {
        OldPreset = oldPreset;
        NewPreset = newPreset;
        Animated = animated;
    }
}

/// <summary>
/// Event arguments for light source updated events.
/// </summary>
public class LightSourceUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the ID of the updated light source.
    /// </summary>
    public Guid LightId { get; }

    /// <summary>
    /// Gets the old light source.
    /// </summary>
    public LightSource OldLight { get; }

    /// <summary>
    /// Gets the new light source.
    /// </summary>
    public LightSource NewLight { get; }

    /// <summary>
    /// Initializes a new instance of the LightSourceUpdatedEventArgs class.
    /// </summary>
    public LightSourceUpdatedEventArgs(Guid lightId, LightSource oldLight, LightSource newLight)
    {
        LightId = lightId;
        OldLight = oldLight;
        NewLight = newLight;
    }
}
