using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Enumeration of light source types.
/// </summary>
public enum LightType
{
    /// <summary>
    /// Ambient light - provides uniform illumination from all directions.
    /// </summary>
    Ambient,

    /// <summary>
    /// Directional light - simulates distant light sources like the sun.
    /// </summary>
    Directional,

    /// <summary>
    /// Point light - emits light from a single point in all directions.
    /// </summary>
    Point,

    /// <summary>
    /// Spot light - emits light in a cone-shaped beam.
    /// </summary>
    Spot
}

/// <summary>
/// Represents a light source in the 3D scene.
/// </summary>
public sealed class LightSource : IEquatable<LightSource>
{
    /// <summary>
    /// Gets the unique identifier for this light source.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the name of this light source.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of this light source.
    /// </summary>
    public LightType Type { get; }

    /// <summary>
    /// Gets whether this light source is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the position of the light (for Point and Spot lights).
    /// </summary>
    public Vector3? Position { get; }

    /// <summary>
    /// Gets the direction of the light (for Directional and Spot lights).
    /// </summary>
    public Vector3? Direction { get; }

    /// <summary>
    /// Gets the color of the light.
    /// </summary>
    public Color Color { get; }

    /// <summary>
    /// Gets the intensity of the light (0.0 to 1.0).
    /// </summary>
    public float Intensity { get; }

    /// <summary>
    /// Gets the range of the light (for Point and Spot lights).
    /// </summary>
    public float? Range { get; }

    /// <summary>
    /// Gets the inner cone angle for spot lights (in radians).
    /// </summary>
    public float? InnerConeAngle { get; }

    /// <summary>
    /// Gets the outer cone angle for spot lights (in radians).
    /// </summary>
    public float? OuterConeAngle { get; }

    /// <summary>
    /// Gets the constant attenuation factor.
    /// </summary>
    public float ConstantAttenuation { get; }

    /// <summary>
    /// Gets the linear attenuation factor.
    /// </summary>
    public float LinearAttenuation { get; }

    /// <summary>
    /// Gets the quadratic attenuation factor.
    /// </summary>
    public float QuadraticAttenuation { get; }

    private LightSource(
        Guid id,
        string name,
        LightType type,
        bool isEnabled,
        Vector3? position,
        Vector3? direction,
        Color color,
        float intensity,
        float? range,
        float? innerConeAngle,
        float? outerConeAngle,
        float constantAttenuation,
        float linearAttenuation,
        float quadraticAttenuation)
    {
        Id = id;
        Name = name;
        Type = type;
        IsEnabled = isEnabled;
        Position = position;
        Direction = direction;
        Color = color;
        Intensity = intensity;
        Range = range;
        InnerConeAngle = innerConeAngle;
        OuterConeAngle = outerConeAngle;
        ConstantAttenuation = constantAttenuation;
        LinearAttenuation = linearAttenuation;
        QuadraticAttenuation = quadraticAttenuation;
    }

    /// <summary>
    /// Creates an ambient light source.
    /// </summary>
    /// <param name="name">The name of the light.</param>
    /// <param name="color">The color of the light.</param>
    /// <param name="intensity">The intensity of the light.</param>
    /// <param name="isEnabled">Whether the light is enabled.</param>
    /// <returns>A new ambient light source.</returns>
    public static LightSource CreateAmbient(string name, Color color, float intensity = 0.2f, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Light name cannot be null or empty.", nameof(name));
        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 1.");

        return new LightSource(
            Guid.NewGuid(),
            name,
            LightType.Ambient,
            isEnabled,
            null,
            null,
            color,
            intensity,
            null,
            null,
            null,
            1.0f,
            0.0f,
            0.0f);
    }

    /// <summary>
    /// Creates a directional light source.
    /// </summary>
    /// <param name="name">The name of the light.</param>
    /// <param name="direction">The direction of the light.</param>
    /// <param name="color">The color of the light.</param>
    /// <param name="intensity">The intensity of the light.</param>
    /// <param name="isEnabled">Whether the light is enabled.</param>
    /// <returns>A new directional light source.</returns>
    public static LightSource CreateDirectional(string name, Vector3 direction, Color color, float intensity = 0.8f, bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Light name cannot be null or empty.", nameof(name));
        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 1.");

        return new LightSource(
            Guid.NewGuid(),
            name,
            LightType.Directional,
            isEnabled,
            null,
            direction.Normalized(),
            color,
            intensity,
            null,
            null,
            null,
            1.0f,
            0.0f,
            0.0f);
    }

    /// <summary>
    /// Creates a point light source.
    /// </summary>
    /// <param name="name">The name of the light.</param>
    /// <param name="position">The position of the light.</param>
    /// <param name="color">The color of the light.</param>
    /// <param name="intensity">The intensity of the light.</param>
    /// <param name="range">The range of the light.</param>
    /// <param name="constantAttenuation">The constant attenuation factor.</param>
    /// <param name="linearAttenuation">The linear attenuation factor.</param>
    /// <param name="quadraticAttenuation">The quadratic attenuation factor.</param>
    /// <param name="isEnabled">Whether the light is enabled.</param>
    /// <returns>A new point light source.</returns>
    public static LightSource CreatePoint(
        string name,
        Vector3 position,
        Color color,
        float intensity = 1.0f,
        float range = 10.0f,
        float constantAttenuation = 1.0f,
        float linearAttenuation = 0.09f,
        float quadraticAttenuation = 0.032f,
        bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Light name cannot be null or empty.", nameof(name));
        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 1.");
        if (range <= 0)
            throw new ArgumentOutOfRangeException(nameof(range), "Range must be greater than 0.");

        return new LightSource(
            Guid.NewGuid(),
            name,
            LightType.Point,
            isEnabled,
            position,
            null,
            color,
            intensity,
            range,
            null,
            null,
            constantAttenuation,
            linearAttenuation,
            quadraticAttenuation);
    }

    /// <summary>
    /// Creates a spot light source.
    /// </summary>
    /// <param name="name">The name of the light.</param>
    /// <param name="position">The position of the light.</param>
    /// <param name="direction">The direction of the light.</param>
    /// <param name="color">The color of the light.</param>
    /// <param name="intensity">The intensity of the light.</param>
    /// <param name="range">The range of the light.</param>
    /// <param name="innerConeAngle">The inner cone angle in radians.</param>
    /// <param name="outerConeAngle">The outer cone angle in radians.</param>
    /// <param name="constantAttenuation">The constant attenuation factor.</param>
    /// <param name="linearAttenuation">The linear attenuation factor.</param>
    /// <param name="quadraticAttenuation">The quadratic attenuation factor.</param>
    /// <param name="isEnabled">Whether the light is enabled.</param>
    /// <returns>A new spot light source.</returns>
    public static LightSource CreateSpot(
        string name,
        Vector3 position,
        Vector3 direction,
        Color color,
        float intensity = 1.0f,
        float range = 10.0f,
        float innerConeAngle = 0.39f, // ~22.5 degrees
        float outerConeAngle = 0.52f, // ~30 degrees
        float constantAttenuation = 1.0f,
        float linearAttenuation = 0.09f,
        float quadraticAttenuation = 0.032f,
        bool isEnabled = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Light name cannot be null or empty.", nameof(name));
        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 1.");
        if (range <= 0)
            throw new ArgumentOutOfRangeException(nameof(range), "Range must be greater than 0.");
        if (innerConeAngle <= 0 || innerConeAngle >= MathF.PI)
            throw new ArgumentOutOfRangeException(nameof(innerConeAngle), "Inner cone angle must be between 0 and PI.");
        if (outerConeAngle <= innerConeAngle || outerConeAngle >= MathF.PI)
            throw new ArgumentOutOfRangeException(nameof(outerConeAngle), "Outer cone angle must be greater than inner cone angle and less than PI.");

        return new LightSource(
            Guid.NewGuid(),
            name,
            LightType.Spot,
            isEnabled,
            position,
            direction.Normalized(),
            color,
            intensity,
            range,
            innerConeAngle,
            outerConeAngle,
            constantAttenuation,
            linearAttenuation,
            quadraticAttenuation);
    }

    /// <summary>
    /// Creates a copy of this light source with the specified enabled state.
    /// </summary>
    /// <param name="isEnabled">The new enabled state.</param>
    /// <returns>A new light source with the updated enabled state.</returns>
    public LightSource WithEnabled(bool isEnabled)
    {
        return new LightSource(
            Id,
            Name,
            Type,
            isEnabled,
            Position,
            Direction,
            Color,
            Intensity,
            Range,
            InnerConeAngle,
            OuterConeAngle,
            ConstantAttenuation,
            LinearAttenuation,
            QuadraticAttenuation);
    }

    /// <summary>
    /// Creates a copy of this light source with the specified intensity.
    /// </summary>
    /// <param name="intensity">The new intensity (0.0 to 1.0).</param>
    /// <returns>A new light source with the updated intensity.</returns>
    public LightSource WithIntensity(float intensity)
    {
        if (intensity < 0 || intensity > 1)
            throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 1.");

        return new LightSource(
            Id,
            Name,
            Type,
            IsEnabled,
            Position,
            Direction,
            Color,
            intensity,
            Range,
            InnerConeAngle,
            OuterConeAngle,
            ConstantAttenuation,
            LinearAttenuation,
            QuadraticAttenuation);
    }

    /// <summary>
    /// Creates a copy of this light source with the specified color.
    /// </summary>
    /// <param name="color">The new color.</param>
    /// <returns>A new light source with the updated color.</returns>
    public LightSource WithColor(Color color)
    {
        return new LightSource(
            Id,
            Name,
            Type,
            IsEnabled,
            Position,
            Direction,
            color,
            Intensity,
            Range,
            InnerConeAngle,
            OuterConeAngle,
            ConstantAttenuation,
            LinearAttenuation,
            QuadraticAttenuation);
    }

    /// <summary>
    /// Creates a copy of this light source with the specified position (for Point and Spot lights).
    /// </summary>
    /// <param name="position">The new position.</param>
    /// <returns>A new light source with the updated position.</returns>
    public LightSource WithPosition(Vector3 position)
    {
        if (Type != LightType.Point && Type != LightType.Spot)
            throw new InvalidOperationException($"Cannot set position on {Type} light.");

        return new LightSource(
            Id,
            Name,
            Type,
            IsEnabled,
            position,
            Direction,
            Color,
            Intensity,
            Range,
            InnerConeAngle,
            OuterConeAngle,
            ConstantAttenuation,
            LinearAttenuation,
            QuadraticAttenuation);
    }

    /// <summary>
    /// Creates a copy of this light source with the specified direction (for Directional and Spot lights).
    /// </summary>
    /// <param name="direction">The new direction.</param>
    /// <returns>A new light source with the updated direction.</returns>
    public LightSource WithDirection(Vector3 direction)
    {
        if (Type != LightType.Directional && Type != LightType.Spot)
            throw new InvalidOperationException($"Cannot set direction on {Type} light.");

        return new LightSource(
            Id,
            Name,
            Type,
            IsEnabled,
            Position,
            direction.Normalized(),
            Color,
            Intensity,
            Range,
            InnerConeAngle,
            OuterConeAngle,
            ConstantAttenuation,
            LinearAttenuation,
            QuadraticAttenuation);
    }

    /// <summary>
    /// Gets the effective color of the light (color * intensity).
    /// </summary>
    public Color EffectiveColor => new Color(
        Color.R * Intensity,
        Color.G * Intensity,
        Color.B * Intensity,
        Color.A);

    /// <inheritdoc/>
    public bool Equals(LightSource? other)
    {
        return other != null && Id == other.Id;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as LightSource);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{Type} Light '{Name}' (Enabled: {IsEnabled}, Intensity: {Intensity:F2})";
    }

    public static bool operator ==(LightSource? left, LightSource? right)
    {
        return EqualityComparer<LightSource>.Default.Equals(left, right);
    }

    public static bool operator !=(LightSource? left, LightSource? right)
    {
        return !(left == right);
    }
}
