using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents material properties for rendering scene objects.
/// </summary>
public sealed record SceneObjectMaterial
{
    /// <summary>
    /// Gets the diffuse color of the material.
    /// </summary>
    public Color DiffuseColor { get; init; } = Color.Gray;

    /// <summary>
    /// Gets the ambient color of the material.
    /// </summary>
    public Color AmbientColor { get; init; } = Color.Black;

    /// <summary>
    /// Gets the specular color of the material.
    /// </summary>
    public Color SpecularColor { get; init; } = Color.White;

    /// <summary>
    /// Gets the emissive color of the material.
    /// </summary>
    public Color EmissiveColor { get; init; } = Color.Black;

    /// <summary>
    /// Gets the transparency level (0.0 = fully transparent, 1.0 = fully opaque).
    /// </summary>
    public float Alpha { get; init; } = 1.0f;

    /// <summary>
    /// Gets the shininess/specular power of the material.
    /// </summary>
    public float Shininess { get; init; } = 32.0f;

    /// <summary>
    /// Gets the metallic factor of the material (0.0 = non-metallic, 1.0 = fully metallic).
    /// </summary>
    public float Metallic { get; init; } = 0.0f;

    /// <summary>
    /// Gets the roughness factor of the material (0.0 = smooth/glossy, 1.0 = rough/matte).
    /// </summary>
    public float Roughness { get; init; } = 0.5f;

    /// <summary>
    /// Gets a value indicating whether the material should receive shadows.
    /// </summary>
    public bool ReceiveShadows { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the material should cast shadows.
    /// </summary>
    public bool CastShadows { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the material uses transparency.
    /// </summary>
    public bool IsTransparent => Alpha < 1.0f;

    /// <summary>
    /// Gets a value indicating whether the material is fully opaque.
    /// </summary>
    public bool IsOpaque => Alpha >= 1.0f;

    /// <summary>
    /// Gets the default material.
    /// </summary>
    public static SceneObjectMaterial Default => new()
    {
        DiffuseColor = Color.Gray,
        AmbientColor = new Color(0.2f, 0.2f, 0.2f, 1.0f),
        SpecularColor = Color.White,
        EmissiveColor = Color.Black,
        Alpha = 1.0f,
        Shininess = 32.0f,
        Metallic = 0.0f,
        Roughness = 0.5f,
        ReceiveShadows = true,
        CastShadows = true
    };

    private SceneObjectMaterial() { }

    /// <summary>
    /// Creates a new material with the specified properties.
    /// </summary>
    /// <param name="diffuseColor">The diffuse color.</param>
    /// <param name="ambientColor">The ambient color (optional).</param>
    /// <param name="specularColor">The specular color (optional).</param>
    /// <param name="emissiveColor">The emissive color (optional).</param>
    /// <param name="alpha">The transparency level (optional).</param>
    /// <param name="shininess">The shininess factor (optional).</param>
    /// <param name="metallic">The metallic factor (optional).</param>
    /// <param name="roughness">The roughness factor (optional).</param>
    /// <param name="receiveShadows">Whether to receive shadows (optional).</param>
    /// <param name="castShadows">Whether to cast shadows (optional).</param>
    /// <returns>A new material instance.</returns>
    public static SceneObjectMaterial Create(
        Color diffuseColor,
        Color? ambientColor = null,
        Color? specularColor = null,
        Color? emissiveColor = null,
        float alpha = 1.0f,
        float shininess = 32.0f,
        float metallic = 0.0f,
        float roughness = 0.5f,
        bool receiveShadows = true,
        bool castShadows = true)
    {
        if (alpha < 0.0f || alpha > 1.0f)
            throw new ArgumentException("Alpha must be between 0.0 and 1.0.", nameof(alpha));

        if (shininess < 0.0f)
            throw new ArgumentException("Shininess must be non-negative.", nameof(shininess));

        if (metallic < 0.0f || metallic > 1.0f)
            throw new ArgumentException("Metallic must be between 0.0 and 1.0.", nameof(metallic));

        if (roughness < 0.0f || roughness > 1.0f)
            throw new ArgumentException("Roughness must be between 0.0 and 1.0.", nameof(roughness));

        return new SceneObjectMaterial
        {
            DiffuseColor = diffuseColor,
            AmbientColor = ambientColor ?? new Color(diffuseColor.R * 0.2f, diffuseColor.G * 0.2f, diffuseColor.B * 0.2f, diffuseColor.A),
            SpecularColor = specularColor ?? Color.White,
            EmissiveColor = emissiveColor ?? Color.Black,
            Alpha = alpha,
            Shininess = shininess,
            Metallic = metallic,
            Roughness = roughness,
            ReceiveShadows = receiveShadows,
            CastShadows = castShadows
        };
    }

    /// <summary>
    /// Creates a material from a preset.
    /// </summary>
    /// <param name="preset">The material preset.</param>
    /// <param name="baseColor">The base color to apply (optional).</param>
    /// <returns>A new material instance.</returns>
    public static SceneObjectMaterial FromPreset(MaterialPreset preset, Color? baseColor = null)
    {
        var color = baseColor ?? Color.Gray;

        return preset switch
        {
            MaterialPreset.Default => Default,
            MaterialPreset.Metal => Create(
                diffuseColor: color,
                metallic: 1.0f,
                roughness: 0.1f,
                shininess: 128.0f),
            MaterialPreset.Plastic => Create(
                diffuseColor: color,
                metallic: 0.0f,
                roughness: 0.7f,
                shininess: 16.0f),
            MaterialPreset.Rubber => Create(
                diffuseColor: color,
                metallic: 0.0f,
                roughness: 0.9f,
                shininess: 4.0f),
            MaterialPreset.Glass => Create(
                diffuseColor: new Color(color.R, color.G, color.B, 0.3f),
                alpha: 0.3f,
                metallic: 0.0f,
                roughness: 0.05f,
                shininess: 256.0f),
            MaterialPreset.Ceramic => Create(
                diffuseColor: color,
                metallic: 0.0f,
                roughness: 0.3f,
                shininess: 64.0f),
            MaterialPreset.Wood => Create(
                diffuseColor: color,
                metallic: 0.0f,
                roughness: 0.8f,
                shininess: 8.0f),
            MaterialPreset.Fabric => Create(
                diffuseColor: color,
                metallic: 0.0f,
                roughness: 0.95f,
                shininess: 2.0f),
            _ => Default
        };
    }

    /// <summary>
    /// Creates a new material with modified properties.
    /// </summary>
    /// <param name="diffuseColor">The new diffuse color (optional).</param>
    /// <param name="ambientColor">The new ambient color (optional).</param>
    /// <param name="specularColor">The new specular color (optional).</param>
    /// <param name="emissiveColor">The new emissive color (optional).</param>
    /// <param name="alpha">The new transparency level (optional).</param>
    /// <param name="shininess">The new shininess factor (optional).</param>
    /// <param name="metallic">The new metallic factor (optional).</param>
    /// <param name="roughness">The new roughness factor (optional).</param>
    /// <param name="receiveShadows">The new receive shadows setting (optional).</param>
    /// <param name="castShadows">The new cast shadows setting (optional).</param>
    /// <returns>A new material instance with the specified changes.</returns>
    public SceneObjectMaterial With(
        Color? diffuseColor = null,
        Color? ambientColor = null,
        Color? specularColor = null,
        Color? emissiveColor = null,
        float? alpha = null,
        float? shininess = null,
        float? metallic = null,
        float? roughness = null,
        bool? receiveShadows = null,
        bool? castShadows = null)
    {
        if (alpha.HasValue && (alpha.Value < 0.0f || alpha.Value > 1.0f))
            throw new ArgumentException("Alpha must be between 0.0 and 1.0.", nameof(alpha));

        if (shininess.HasValue && shininess.Value < 0.0f)
            throw new ArgumentException("Shininess must be non-negative.", nameof(shininess));

        if (metallic.HasValue && (metallic.Value < 0.0f || metallic.Value > 1.0f))
            throw new ArgumentException("Metallic must be between 0.0 and 1.0.", nameof(metallic));

        if (roughness.HasValue && (roughness.Value < 0.0f || roughness.Value > 1.0f))
            throw new ArgumentException("Roughness must be between 0.0 and 1.0.", nameof(roughness));

        return this with
        {
            DiffuseColor = diffuseColor ?? DiffuseColor,
            AmbientColor = ambientColor ?? AmbientColor,
            SpecularColor = specularColor ?? SpecularColor,
            EmissiveColor = emissiveColor ?? EmissiveColor,
            Alpha = alpha ?? Alpha,
            Shininess = shininess ?? Shininess,
            Metallic = metallic ?? Metallic,
            Roughness = roughness ?? Roughness,
            ReceiveShadows = receiveShadows ?? ReceiveShadows,
            CastShadows = castShadows ?? CastShadows
        };
    }
}

/// <summary>
/// Predefined material presets.
/// </summary>
public enum MaterialPreset
{
    Default,
    Metal,
    Plastic,
    Rubber,
    Glass,
    Ceramic,
    Wood,
    Fabric
}
