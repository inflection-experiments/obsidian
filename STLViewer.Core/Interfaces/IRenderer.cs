using STLViewer.Math;
using STLViewer.Domain.Entities;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for 3D graphics renderers.
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Gets the renderer type.
    /// </summary>
    RendererType Type { get; }

    /// <summary>
    /// Gets a value indicating whether the renderer is initialized.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the renderer with the specified dimensions.
    /// </summary>
    /// <param name="width">The viewport width.</param>
    /// <param name="height">The viewport height.</param>
    /// <returns>A task representing the initialization operation.</returns>
    Task InitializeAsync(int width, int height);

    /// <summary>
    /// Resizes the renderer viewport.
    /// </summary>
    /// <param name="width">The new viewport width.</param>
    /// <param name="height">The new viewport height.</param>
    void Resize(int width, int height);

    /// <summary>
    /// Sets the camera for rendering.
    /// </summary>
    /// <param name="camera">The camera to use for rendering.</param>
    void SetCamera(ICamera camera);

    /// <summary>
    /// Renders the specified STL model.
    /// </summary>
    /// <param name="model">The STL model to render.</param>
    /// <param name="renderSettings">The rendering settings.</param>
    void Render(STLModel model, RenderSettings renderSettings);

    /// <summary>
    /// Clears the rendering surface.
    /// </summary>
    /// <param name="clearColor">The color to clear with.</param>
    void Clear(Color clearColor);

    /// <summary>
    /// Presents the rendered frame.
    /// </summary>
    void Present();

    /// <summary>
    /// Gets renderer-specific information.
    /// </summary>
    /// <returns>Information about the renderer.</returns>
    RendererInfo GetInfo();
}

/// <summary>
/// Enumeration of renderer types.
/// </summary>
public enum RendererType
{
    /// <summary>
    /// OpenGL renderer.
    /// </summary>
    OpenGL,

    /// <summary>
    /// Vulkan renderer.
    /// </summary>
    Vulkan
}

/// <summary>
/// Enumeration of rendering modes.
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// Surface rendering (solid fill).
    /// </summary>
    Surface,

    /// <summary>
    /// Wireframe rendering (outline only).
    /// </summary>
    Wireframe,

    /// <summary>
    /// Shaded wireframe rendering (combination).
    /// </summary>
    ShadedWireframe
}

/// <summary>
/// Interface for render mode settings.
/// </summary>
public interface IRenderModeSettings
{
    /// <summary>
    /// Gets the current render mode.
    /// </summary>
    RenderMode RenderMode { get; }
}

/// <summary>
/// Rendering settings for customizing the rendering process.
/// </summary>
public class RenderSettings : IRenderModeSettings
{
    /// <summary>
    /// Gets or sets the rendering mode.
    /// </summary>
    public RenderMode RenderMode { get; set; } = RenderMode.Surface;

    /// <summary>
    /// Gets or sets the wireframe mode.
    /// </summary>
    public bool Wireframe { get; set; } = false;

    /// <summary>
    /// Gets or sets the shading mode.
    /// </summary>
    public ShadingMode Shading { get; set; } = ShadingMode.Smooth;

    /// <summary>
    /// Gets or sets the material properties.
    /// </summary>
    public Material Material { get; set; } = new Material();

    /// <summary>
    /// Gets or sets the model color (legacy property, maps to Material.DiffuseColor).
    /// </summary>
    public Color ModelColor
    {
        get => Material.DiffuseColor;
        set => Material.DiffuseColor = value;
    }

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.DarkGray;

    /// <summary>
    /// Gets or sets whether to show normals.
    /// </summary>
    public bool ShowNormals { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to show bounding box.
    /// </summary>
    public bool ShowBoundingBox { get; set; } = false;

    /// <summary>
    /// Gets or sets the lighting settings.
    /// </summary>
    public LightingSettings Lighting { get; set; } = new();

    /// <summary>
    /// Gets or sets the anti-aliasing level.
    /// </summary>
    public int AntiAliasing { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to use depth testing.
    /// </summary>
    public bool DepthTesting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use backface culling.
    /// </summary>
    public bool BackfaceCulling { get; set; } = true;

    /// <summary>
    /// Gets or sets whether transparency is enabled.
    /// </summary>
    public bool EnableTransparency { get; set; } = false;
}

/// <summary>
/// Shading modes for rendering.
/// </summary>
public enum ShadingMode
{
    /// <summary>
    /// Flat shading.
    /// </summary>
    Flat,

    /// <summary>
    /// Smooth shading.
    /// </summary>
    Smooth
}

/// <summary>
/// Lighting settings for rendering.
/// </summary>
public class LightingSettings
{
    /// <summary>
    /// Gets or sets whether lighting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the ambient light color.
    /// </summary>
    public Color AmbientColor { get; set; } = new(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// Gets or sets the diffuse light color.
    /// </summary>
    public Color DiffuseColor { get; set; } = new(0.8f, 0.8f, 0.8f);

    /// <summary>
    /// Gets or sets the specular light color.
    /// </summary>
    public Color SpecularColor { get; set; } = new(1.0f, 1.0f, 1.0f);

    /// <summary>
    /// Gets or sets the light direction.
    /// </summary>
    public Vector3 LightDirection { get; set; } = new(-1, -1, -1);

    /// <summary>
    /// Gets or sets the shininess factor.
    /// </summary>
    public float Shininess { get; set; } = 32.0f;
}

/// <summary>
/// Information about a renderer.
/// </summary>
public class RendererInfo
{
    /// <summary>
    /// Gets or sets the renderer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the renderer version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the graphics API version.
    /// </summary>
    public string ApiVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the graphics device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the vendor name.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional capabilities.
    /// </summary>
    public Dictionary<string, object> Capabilities { get; set; } = new();
}

/// <summary>
/// Enumeration of material presets.
/// </summary>
public enum MaterialPreset
{
    /// <summary>
    /// Default material.
    /// </summary>
    Default,

    /// <summary>
    /// Metallic material with high reflectivity.
    /// </summary>
    Metal,

    /// <summary>
    /// Plastic material with medium reflectivity.
    /// </summary>
    Plastic,

    /// <summary>
    /// Matte material with low reflectivity.
    /// </summary>
    Matte,

    /// <summary>
    /// Glossy material with high shininess.
    /// </summary>
    Glossy,

    /// <summary>
    /// Custom material with user-defined properties.
    /// </summary>
    Custom
}

/// <summary>
/// Represents material properties for 3D rendering.
/// </summary>
public class Material
{
    /// <summary>
    /// Gets or sets the diffuse color (base color).
    /// </summary>
    public Color DiffuseColor { get; set; } = Color.LightGray;

    /// <summary>
    /// Gets or sets the ambient color.
    /// </summary>
    public Color AmbientColor { get; set; } = new Color(0.2f, 0.2f, 0.2f);

    /// <summary>
    /// Gets or sets the specular color.
    /// </summary>
    public Color SpecularColor { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the emissive color.
    /// </summary>
    public Color EmissiveColor { get; set; } = Color.Black;

    /// <summary>
    /// Gets or sets the shininess factor (specular exponent).
    /// </summary>
    public float Shininess { get; set; } = 32.0f;

    /// <summary>
    /// Gets or sets the transparency (0.0 = fully transparent, 1.0 = fully opaque).
    /// </summary>
    public float Alpha { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the metallic factor (0.0 = dielectric, 1.0 = metallic).
    /// </summary>
    public float Metallic { get; set; } = 0.0f;

    /// <summary>
    /// Gets or sets the roughness factor (0.0 = mirror, 1.0 = completely rough).
    /// </summary>
    public float Roughness { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the material preset type.
    /// </summary>
    public MaterialPreset Preset { get; set; } = MaterialPreset.Default;

    /// <summary>
    /// Creates a material from a preset.
    /// </summary>
    public static Material FromPreset(MaterialPreset preset, Color? baseColor = null)
    {
        var color = baseColor ?? Color.LightGray;

        return preset switch
        {
            MaterialPreset.Metal => new Material
            {
                DiffuseColor = color,
                AmbientColor = new Color(color.R * 0.1f, color.G * 0.1f, color.B * 0.1f, color.A),
                SpecularColor = Color.White,
                Shininess = 128.0f,
                Metallic = 0.9f,
                Roughness = 0.1f,
                Alpha = 1.0f,
                Preset = preset
            },
            MaterialPreset.Plastic => new Material
            {
                DiffuseColor = color,
                AmbientColor = new Color(color.R * 0.2f, color.G * 0.2f, color.B * 0.2f, color.A),
                SpecularColor = new Color(0.5f, 0.5f, 0.5f, 1.0f),
                Shininess = 64.0f,
                Metallic = 0.0f,
                Roughness = 0.3f,
                Alpha = 1.0f,
                Preset = preset
            },
            MaterialPreset.Matte => new Material
            {
                DiffuseColor = color,
                AmbientColor = new Color(color.R * 0.3f, color.G * 0.3f, color.B * 0.3f, color.A),
                SpecularColor = Color.Black,
                Shininess = 1.0f,
                Metallic = 0.0f,
                Roughness = 1.0f,
                Alpha = 1.0f,
                Preset = preset
            },
            MaterialPreset.Glossy => new Material
            {
                DiffuseColor = color,
                AmbientColor = new Color(color.R * 0.1f, color.G * 0.1f, color.B * 0.1f, color.A),
                SpecularColor = Color.White,
                Shininess = 256.0f,
                Metallic = 0.2f,
                Roughness = 0.05f,
                Alpha = 1.0f,
                Preset = preset
            },
            MaterialPreset.Custom => new Material
            {
                DiffuseColor = color,
                Preset = preset
            },
            _ => new Material
            {
                DiffuseColor = color,
                Preset = MaterialPreset.Default
            }
        };
    }

    /// <summary>
    /// Creates a copy of this material.
    /// </summary>
    public Material Clone()
    {
        return new Material
        {
            DiffuseColor = DiffuseColor,
            AmbientColor = AmbientColor,
            SpecularColor = SpecularColor,
            EmissiveColor = EmissiveColor,
            Shininess = Shininess,
            Alpha = Alpha,
            Metallic = Metallic,
            Roughness = Roughness,
            Preset = Preset
        };
    }
}
