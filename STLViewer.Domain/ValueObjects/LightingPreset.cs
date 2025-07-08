using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Enumeration of predefined lighting preset types.
/// </summary>
public enum LightingPresetType
{
    /// <summary>
    /// Basic lighting with ambient and one directional light.
    /// </summary>
    Basic,

    /// <summary>
    /// Studio lighting setup with multiple directional lights.
    /// </summary>
    Studio,

    /// <summary>
    /// Outdoor lighting simulating sunlight and sky.
    /// </summary>
    Outdoor,

    /// <summary>
    /// Indoor lighting with warm ambient and soft directional lights.
    /// </summary>
    Indoor,

    /// <summary>
    /// High contrast lighting for technical visualization.
    /// </summary>
    Technical,

    /// <summary>
    /// Dramatic lighting with strong directional lights and deep shadows.
    /// </summary>
    Dramatic,

    /// <summary>
    /// Soft uniform lighting for material showcasing.
    /// </summary>
    Showcase,

    /// <summary>
    /// Custom user-defined lighting setup.
    /// </summary>
    Custom
}

/// <summary>
/// Represents a predefined lighting configuration with multiple light sources.
/// </summary>
public sealed class LightingPreset : IEquatable<LightingPreset>
{
    /// <summary>
    /// Gets the unique identifier for this lighting preset.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of this lighting preset.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the description of this lighting preset.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the type of this lighting preset.
    /// </summary>
    public LightingPresetType Type { get; }

    /// <summary>
    /// Gets the light sources in this preset.
    /// </summary>
    public IReadOnlyList<LightSource> LightSources { get; }

    /// <summary>
    /// Gets the global ambient light intensity.
    /// </summary>
    public float GlobalAmbientIntensity { get; }

    /// <summary>
    /// Gets the global ambient light color.
    /// </summary>
    public Color GlobalAmbientColor { get; }

    private LightingPreset(
        Guid id,
        string name,
        string description,
        LightingPresetType type,
        IReadOnlyList<LightSource> lightSources,
        float globalAmbientIntensity,
        Color globalAmbientColor)
    {
        Id = id;
        Name = name;
        Description = description;
        Type = type;
        LightSources = lightSources;
        GlobalAmbientIntensity = globalAmbientIntensity;
        GlobalAmbientColor = globalAmbientColor;
    }

    /// <summary>
    /// Creates a custom lighting preset.
    /// </summary>
    /// <param name="name">The name of the preset.</param>
    /// <param name="description">The description of the preset.</param>
    /// <param name="lightSources">The light sources in the preset.</param>
    /// <param name="globalAmbientIntensity">The global ambient light intensity.</param>
    /// <param name="globalAmbientColor">The global ambient light color.</param>
    /// <returns>A new custom lighting preset.</returns>
    public static LightingPreset CreateCustom(
        string name,
        string description,
        IEnumerable<LightSource> lightSources,
        float globalAmbientIntensity = 0.1f,
        Color? globalAmbientColor = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Preset name cannot be null or empty.", nameof(name));
        if (globalAmbientIntensity < 0 || globalAmbientIntensity > 1)
            throw new ArgumentOutOfRangeException(nameof(globalAmbientIntensity), "Global ambient intensity must be between 0 and 1.");

        return new LightingPreset(
            Guid.NewGuid(),
            name,
            description ?? "",
            LightingPresetType.Custom,
            lightSources.ToList().AsReadOnly(),
            globalAmbientIntensity,
            globalAmbientColor ?? new Color(0.1f, 0.1f, 0.1f));
    }

    /// <summary>
    /// Creates a basic lighting preset with ambient and one directional light.
    /// </summary>
    public static LightingPreset CreateBasic()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Ambient", new Color(0.2f, 0.2f, 0.2f), 0.2f),
            LightSource.CreateDirectional("Main Light", new Vector3(-0.5f, -1.0f, -0.5f), Color.White, 0.8f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Basic",
            "Simple lighting with ambient and one directional light",
            LightingPresetType.Basic,
            lightSources.AsReadOnly(),
            0.1f,
            new Color(0.1f, 0.1f, 0.1f));
    }

    /// <summary>
    /// Creates a studio lighting preset with three-point lighting setup.
    /// </summary>
    public static LightingPreset CreateStudio()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Studio Ambient", new Color(0.15f, 0.15f, 0.15f), 0.15f),
            LightSource.CreateDirectional("Key Light", new Vector3(-0.5f, -0.8f, -0.3f), Color.White, 1.0f),
            LightSource.CreateDirectional("Fill Light", new Vector3(0.7f, -0.3f, -0.2f), new Color(0.9f, 0.9f, 1.0f), 0.4f),
            LightSource.CreateDirectional("Rim Light", new Vector3(0.2f, 0.5f, 0.8f), new Color(1.0f, 0.95f, 0.9f), 0.6f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Studio",
            "Professional three-point lighting setup",
            LightingPresetType.Studio,
            lightSources.AsReadOnly(),
            0.05f,
            new Color(0.05f, 0.05f, 0.05f));
    }

    /// <summary>
    /// Creates an outdoor lighting preset simulating sunlight.
    /// </summary>
    public static LightingPreset CreateOutdoor()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Sky Light", new Color(0.4f, 0.6f, 1.0f), 0.3f),
            LightSource.CreateDirectional("Sun", new Vector3(-0.3f, -0.8f, -0.5f), new Color(1.0f, 0.95f, 0.8f), 1.0f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Outdoor",
            "Natural outdoor lighting with sun and sky",
            LightingPresetType.Outdoor,
            lightSources.AsReadOnly(),
            0.2f,
            new Color(0.2f, 0.3f, 0.4f));
    }

    /// <summary>
    /// Creates an indoor lighting preset with warm lighting.
    /// </summary>
    public static LightingPreset CreateIndoor()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Room Ambient", new Color(0.3f, 0.25f, 0.2f), 0.25f),
            LightSource.CreateDirectional("Window Light", new Vector3(-0.6f, -0.6f, -0.3f), new Color(0.9f, 0.9f, 1.0f), 0.6f),
            LightSource.CreateDirectional("Ceiling Light", new Vector3(0.1f, -1.0f, 0.1f), new Color(1.0f, 0.9f, 0.7f), 0.5f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Indoor",
            "Warm indoor lighting with window and ceiling lights",
            LightingPresetType.Indoor,
            lightSources.AsReadOnly(),
            0.15f,
            new Color(0.15f, 0.12f, 0.1f));
    }

    /// <summary>
    /// Creates a technical lighting preset for CAD-style visualization.
    /// </summary>
    public static LightingPreset CreateTechnical()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Technical Ambient", Color.White, 0.4f),
            LightSource.CreateDirectional("Primary", new Vector3(-0.4f, -0.8f, -0.4f), Color.White, 0.8f),
            LightSource.CreateDirectional("Secondary", new Vector3(0.6f, -0.5f, -0.2f), Color.White, 0.5f),
            LightSource.CreateDirectional("Tertiary", new Vector3(0.0f, -0.3f, 0.9f), Color.White, 0.3f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Technical",
            "High contrast lighting for technical visualization",
            LightingPresetType.Technical,
            lightSources.AsReadOnly(),
            0.3f,
            Color.White);
    }

    /// <summary>
    /// Creates a dramatic lighting preset with strong shadows.
    /// </summary>
    public static LightingPreset CreateDramatic()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Dramatic Ambient", new Color(0.05f, 0.05f, 0.1f), 0.05f),
            LightSource.CreateDirectional("Dramatic Key", new Vector3(-0.8f, -0.5f, -0.2f), Color.White, 1.0f),
            LightSource.CreateDirectional("Accent", new Vector3(0.3f, 0.7f, 0.6f), new Color(0.8f, 0.9f, 1.0f), 0.3f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Dramatic",
            "High contrast dramatic lighting with strong shadows",
            LightingPresetType.Dramatic,
            lightSources.AsReadOnly(),
            0.02f,
            new Color(0.02f, 0.02f, 0.05f));
    }

    /// <summary>
    /// Creates a showcase lighting preset for material demonstration.
    /// </summary>
    public static LightingPreset CreateShowcase()
    {
        var lightSources = new List<LightSource>
        {
            LightSource.CreateAmbient("Showcase Ambient", Color.White, 0.3f),
            LightSource.CreateDirectional("Main", new Vector3(-0.3f, -0.7f, -0.4f), Color.White, 0.7f),
            LightSource.CreateDirectional("Fill", new Vector3(0.5f, -0.4f, -0.3f), Color.White, 0.4f),
            LightSource.CreateDirectional("Top", new Vector3(0.0f, -1.0f, 0.0f), Color.White, 0.3f)
        };

        return new LightingPreset(
            Guid.NewGuid(),
            "Showcase",
            "Soft uniform lighting perfect for material showcasing",
            LightingPresetType.Showcase,
            lightSources.AsReadOnly(),
            0.2f,
            new Color(0.2f, 0.2f, 0.2f));
    }

    /// <summary>
    /// Gets all predefined lighting presets.
    /// </summary>
    public static IEnumerable<LightingPreset> GetPredefinedPresets()
    {
        yield return CreateBasic();
        yield return CreateStudio();
        yield return CreateOutdoor();
        yield return CreateIndoor();
        yield return CreateTechnical();
        yield return CreateDramatic();
        yield return CreateShowcase();
    }

    /// <summary>
    /// Creates a copy of this preset with the specified light sources.
    /// </summary>
    /// <param name="lightSources">The new light sources.</param>
    /// <returns>A new lighting preset with the updated light sources.</returns>
    public LightingPreset WithLightSources(IEnumerable<LightSource> lightSources)
    {
        return new LightingPreset(
            Id,
            Name,
            Description,
            LightingPresetType.Custom,
            lightSources.ToList().AsReadOnly(),
            GlobalAmbientIntensity,
            GlobalAmbientColor);
    }

    /// <summary>
    /// Creates a copy of this preset with the specified global ambient settings.
    /// </summary>
    /// <param name="intensity">The global ambient intensity.</param>
    /// <param name="color">The global ambient color.</param>
    /// <returns>A new lighting preset with the updated global ambient settings.</returns>
    public LightingPreset WithGlobalAmbient(float intensity, Color color)
    {
        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 1.");

        return new LightingPreset(
            Id,
            Name,
            Description,
            Type,
            LightSources,
            intensity,
            color);
    }

    /// <summary>
    /// Gets the total number of enabled lights in this preset.
    /// </summary>
    public int EnabledLightCount => LightSources.Count(l => l.IsEnabled);

    /// <summary>
    /// Gets the light sources of a specific type.
    /// </summary>
    /// <param name="lightType">The type of lights to retrieve.</param>
    /// <returns>The light sources of the specified type.</returns>
    public IEnumerable<LightSource> GetLightsByType(LightType lightType)
    {
        return LightSources.Where(l => l.Type == lightType);
    }

    /// <inheritdoc/>
    public bool Equals(LightingPreset? other)
    {
        return other != null && Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as LightingPreset);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Type} Preset '{Name}' ({EnabledLightCount}/{LightSources.Count} lights enabled)";
    }

    public static bool operator ==(LightingPreset? left, LightingPreset? right)
    {
        return EqualityComparer<LightingPreset>.Default.Equals(left, right);
    }

    public static bool operator !=(LightingPreset? left, LightingPreset? right)
    {
        return !(left == right);
    }
}
