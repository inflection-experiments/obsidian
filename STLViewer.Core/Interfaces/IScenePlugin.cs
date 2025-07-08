using STLViewer.Domain.Common;
using STLViewer.Math;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for scene visualization plugins that extend the STL viewer with additional features.
/// </summary>
public interface IScenePlugin
{
    /// <summary>
    /// Gets the unique identifier for this plugin.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of this plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this plugin.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the version of this plugin.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets whether this plugin is currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets whether this plugin supports configuration.
    /// </summary>
    bool IsConfigurable { get; }

    /// <summary>
    /// Initializes the plugin.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result Initialize();

    /// <summary>
    /// Shuts down the plugin and cleans up resources.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result Shutdown();

    /// <summary>
    /// Enables the plugin.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result Enable();

    /// <summary>
    /// Disables the plugin.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result Disable();

    /// <summary>
    /// Updates the plugin for the current frame.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    void Update(float deltaTime);

    /// <summary>
    /// Renders the plugin's visual elements.
    /// </summary>
    /// <param name="renderer">The renderer to use for drawing.</param>
    /// <param name="camera">The current camera.</param>
    void Render(IRenderer renderer, ICamera camera);

    /// <summary>
    /// Gets configuration options for this plugin.
    /// </summary>
    /// <returns>Configuration data for the plugin.</returns>
    Dictionary<string, object> GetConfiguration();

    /// <summary>
    /// Sets configuration options for this plugin.
    /// </summary>
    /// <param name="configuration">The configuration data to apply.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result SetConfiguration(Dictionary<string, object> configuration);

    /// <summary>
    /// Handles mouse input events.
    /// </summary>
    /// <param name="mouseX">The mouse X coordinate.</param>
    /// <param name="mouseY">The mouse Y coordinate.</param>
    /// <param name="isPressed">Whether the mouse button is pressed.</param>
    /// <param name="camera">The current camera.</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    bool HandleMouseInput(float mouseX, float mouseY, bool isPressed, ICamera camera);

    /// <summary>
    /// Handles keyboard input events.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="isPressed">Whether the key is pressed (true) or released (false).</param>
    /// <returns>True if the event was handled, false otherwise.</returns>
    bool HandleKeyboardInput(string key, bool isPressed);
}

/// <summary>
/// Interface for managing scene plugins.
/// </summary>
public interface IScenePluginManager
{
    /// <summary>
    /// Gets all registered plugins.
    /// </summary>
    IEnumerable<IScenePlugin> GetAllPlugins();

    /// <summary>
    /// Gets enabled plugins.
    /// </summary>
    IEnumerable<IScenePlugin> GetEnabledPlugins();

    /// <summary>
    /// Gets a plugin by its ID.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <returns>The plugin if found, null otherwise.</returns>
    IScenePlugin? GetPlugin(string pluginId);

    /// <summary>
    /// Registers a plugin with the manager.
    /// </summary>
    /// <param name="plugin">The plugin to register.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result RegisterPlugin(IScenePlugin plugin);

    /// <summary>
    /// Unregisters a plugin from the manager.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to unregister.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result UnregisterPlugin(string pluginId);

    /// <summary>
    /// Enables a plugin.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to enable.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result EnablePlugin(string pluginId);

    /// <summary>
    /// Disables a plugin.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to disable.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result DisablePlugin(string pluginId);

    /// <summary>
    /// Updates all enabled plugins.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    void UpdatePlugins(float deltaTime);

    /// <summary>
    /// Renders all enabled plugins.
    /// </summary>
    /// <param name="renderer">The renderer to use for drawing.</param>
    /// <param name="camera">The current camera.</param>
    void RenderPlugins(IRenderer renderer, ICamera camera);
}
