namespace STLViewer.Infrastructure.Configuration;

/// <summary>
/// Configuration options for rendering settings.
/// </summary>
public class RenderingOptions
{
    public const string SectionName = "Rendering";

    /// <summary>
    /// Gets or sets the default renderer (OpenGL, Vulkan, etc.).
    /// </summary>
    public string DefaultRenderer { get; set; } = "OpenGL";

    /// <summary>
    /// Gets or sets the maximum texture size.
    /// </summary>
    public int MaxTextureSize { get; set; } = 4096;

    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// </summary>
    public bool EnableVSync { get; set; } = true;

    /// <summary>
    /// Gets or sets the anti-aliasing level.
    /// </summary>
    public int AntiAliasing { get; set; } = 4;

    /// <summary>
    /// Gets or sets the maximum frames per second.
    /// </summary>
    public int MaxFPS { get; set; } = 60;

    /// <summary>
    /// Gets or sets whether wireframe mode is enabled by default.
    /// </summary>
    public bool DefaultWireframe { get; set; } = false;

    /// <summary>
    /// Gets or sets the default background color (as hex string).
    /// </summary>
    public string DefaultBackgroundColor { get; set; } = "#2D2D30";

    /// <summary>
    /// Gets or sets whether depth testing is enabled by default.
    /// </summary>
    public bool DefaultDepthTesting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether backface culling is enabled by default.
    /// </summary>
    public bool DefaultBackfaceCulling { get; set; } = true;
}
