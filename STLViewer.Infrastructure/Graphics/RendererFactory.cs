using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using STLViewer.Core.Interfaces;
using STLViewer.Infrastructure.Graphics.OpenGL;
using STLViewer.Infrastructure.Graphics.Vulkan;

namespace STLViewer.Infrastructure.Graphics;

/// <summary>
/// Factory for creating graphics renderers.
/// </summary>
public static class RendererFactory
{
    /// <summary>
    /// Creates an OpenGL renderer.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    /// <returns>An OpenGL renderer instance.</returns>
    public static IRenderer CreateOpenGLRenderer(GL gl)
    {
        return new OpenGLRenderer(gl);
    }

    /// <summary>
    /// Creates a Vulkan renderer.
    /// </summary>
    /// <param name="vk">The Vulkan API instance.</param>
    /// <returns>A Vulkan renderer instance.</returns>
    public static IRenderer CreateVulkanRenderer(Vk vk)
    {
        return new VulkanRenderer(vk);
    }

    /// <summary>
    /// Checks if OpenGL is available on the current system.
    /// </summary>
    /// <returns>True if OpenGL is available; otherwise, false.</returns>
    public static bool IsOpenGLAvailable()
    {
        try
        {
            // This is a basic check - in a real implementation, you'd need a window context
            // For now, we'll assume OpenGL is available
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if Vulkan is available on the current system.
    /// </summary>
    /// <returns>True if Vulkan is available; otherwise, false.</returns>
    public static unsafe bool IsVulkanAvailable()
    {
        try
        {
            // Try to create a Vulkan instance to check availability
            var vk = Vk.GetApi();
            if (vk == null) return false;

            // Try to enumerate instance extensions as a basic check
            uint extensionCount = 0;
            var result = vk.EnumerateInstanceExtensionProperties((byte*)null, &extensionCount, null);
            vk.Dispose();

            return result == Result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the preferred renderer type based on system capabilities.
    /// </summary>
    /// <returns>The preferred renderer type.</returns>
    public static RendererType GetPreferredRendererType()
    {
        // Prefer Vulkan if available for better performance, otherwise fallback to OpenGL
        if (IsVulkanAvailable())
            return RendererType.Vulkan;

        if (IsOpenGLAvailable())
            return RendererType.OpenGL;

        throw new InvalidOperationException("No supported graphics API found on this system");
    }

    /// <summary>
    /// Gets available renderer types on the current system.
    /// </summary>
    /// <returns>An enumerable of available renderer types.</returns>
    public static IEnumerable<RendererType> GetAvailableRendererTypes()
    {
        var available = new List<RendererType>();

        if (IsOpenGLAvailable())
            available.Add(RendererType.OpenGL);

        if (IsVulkanAvailable())
            available.Add(RendererType.Vulkan);

        return available;
    }

    /// <summary>
    /// Creates a renderer of the specified type.
    /// </summary>
    /// <param name="rendererType">The type of renderer to create.</param>
    /// <param name="graphicsContext">The graphics context (GL for OpenGL, Vk for Vulkan).</param>
    /// <returns>A renderer instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the renderer type is not supported.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the graphics context is null.</exception>
    public static IRenderer CreateRenderer(RendererType rendererType, object graphicsContext)
    {
        if (graphicsContext == null)
            throw new ArgumentNullException(nameof(graphicsContext));

        return rendererType switch
        {
            RendererType.OpenGL when graphicsContext is GL gl => CreateOpenGLRenderer(gl),
            RendererType.Vulkan when graphicsContext is Vk vk => CreateVulkanRenderer(vk),
            _ => throw new ArgumentException($"Unsupported renderer type or invalid graphics context: {rendererType}")
        };
    }
}
