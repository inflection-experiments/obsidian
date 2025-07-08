using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;

namespace STLViewer.Infrastructure.Plugins;

/// <summary>
/// Implementation of the scene plugin manager for managing visualization plugins.
/// </summary>
public class ScenePluginManager : IScenePluginManager
{
    private readonly Dictionary<string, IScenePlugin> _plugins = new();
    private readonly object _lock = new();

    public IEnumerable<IScenePlugin> GetAllPlugins()
    {
        lock (_lock)
        {
            return _plugins.Values.ToArray();
        }
    }

    public IEnumerable<IScenePlugin> GetEnabledPlugins()
    {
        lock (_lock)
        {
            return _plugins.Values.Where(p => p.IsEnabled).ToArray();
        }
    }

    public IScenePlugin? GetPlugin(string pluginId)
    {
        if (string.IsNullOrEmpty(pluginId))
            return null;

        lock (_lock)
        {
            return _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
        }
    }

    public Result RegisterPlugin(IScenePlugin plugin)
    {
        if (plugin == null)
            return Result.Fail("Plugin cannot be null");

        if (string.IsNullOrEmpty(plugin.Id))
            return Result.Fail("Plugin ID cannot be null or empty");

        lock (_lock)
        {
            if (_plugins.ContainsKey(plugin.Id))
                return Result.Fail($"Plugin with ID '{plugin.Id}' is already registered");

            var initResult = plugin.Initialize();
            if (initResult.IsFailure)
                return Result.Fail($"Failed to initialize plugin '{plugin.Id}': {initResult.Error}");

            _plugins[plugin.Id] = plugin;
            return Result.Ok();
        }
    }

    public Result UnregisterPlugin(string pluginId)
    {
        if (string.IsNullOrEmpty(pluginId))
            return Result.Fail("Plugin ID cannot be null or empty");

        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out var plugin))
                return Result.Fail($"Plugin with ID '{pluginId}' is not registered");

            // Shutdown the plugin
            var shutdownResult = plugin.Shutdown();
            if (shutdownResult.IsFailure)
            {
                // Log the error but continue with unregistration
                // In a real implementation, this would use proper logging
                Console.WriteLine($"Warning: Failed to shutdown plugin '{pluginId}': {shutdownResult.Error}");
            }

            _plugins.Remove(pluginId);
            return Result.Ok();
        }
    }

    public Result EnablePlugin(string pluginId)
    {
        if (string.IsNullOrEmpty(pluginId))
            return Result.Fail("Plugin ID cannot be null or empty");

        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out var plugin))
                return Result.Fail($"Plugin with ID '{pluginId}' is not registered");

            var enableResult = plugin.Enable();
            if (enableResult.IsFailure)
                return Result.Fail($"Failed to enable plugin '{pluginId}': {enableResult.Error}");

            return Result.Ok();
        }
    }

    public Result DisablePlugin(string pluginId)
    {
        if (string.IsNullOrEmpty(pluginId))
            return Result.Fail("Plugin ID cannot be null or empty");

        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out var plugin))
                return Result.Fail($"Plugin with ID '{pluginId}' is not registered");

            var disableResult = plugin.Disable();
            if (disableResult.IsFailure)
                return Result.Fail($"Failed to disable plugin '{pluginId}': {disableResult.Error}");

            return Result.Ok();
        }
    }

    public void UpdatePlugins(float deltaTime)
    {
        IScenePlugin[] enabledPlugins;

        lock (_lock)
        {
            enabledPlugins = _plugins.Values.Where(p => p.IsEnabled).ToArray();
        }

        // Update plugins outside the lock to avoid blocking
        foreach (var plugin in enabledPlugins)
        {
            try
            {
                plugin.Update(deltaTime);
            }
            catch (Exception ex)
            {
                // In a real implementation, this would use proper logging
                Console.WriteLine($"Error updating plugin '{plugin.Id}': {ex.Message}");
            }
        }
    }

    public void RenderPlugins(IRenderer renderer, ICamera camera)
    {
        if (renderer == null || camera == null)
            return;

        IScenePlugin[] enabledPlugins;

        lock (_lock)
        {
            enabledPlugins = _plugins.Values.Where(p => p.IsEnabled).ToArray();
        }

        // Render plugins outside the lock to avoid blocking
        foreach (var plugin in enabledPlugins)
        {
            try
            {
                plugin.Render(renderer, camera);
            }
            catch (Exception ex)
            {
                // In a real implementation, this would use proper logging
                Console.WriteLine($"Error rendering plugin '{plugin.Id}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Handles mouse input for all enabled plugins.
    /// </summary>
    /// <param name="mouseX">The mouse X coordinate.</param>
    /// <param name="mouseY">The mouse Y coordinate.</param>
    /// <param name="isPressed">Whether the mouse button is pressed.</param>
    /// <param name="camera">The current camera.</param>
    /// <returns>True if any plugin handled the input, false otherwise.</returns>
    public bool HandleMouseInput(float mouseX, float mouseY, bool isPressed, ICamera camera)
    {
        if (camera == null)
            return false;

        IScenePlugin[] enabledPlugins;

        lock (_lock)
        {
            enabledPlugins = _plugins.Values.Where(p => p.IsEnabled).ToArray();
        }

        // Allow plugins to handle input in registration order
        foreach (var plugin in enabledPlugins)
        {
            try
            {
                if (plugin.HandleMouseInput(mouseX, mouseY, isPressed, camera))
                    return true; // Input was handled
            }
            catch (Exception ex)
            {
                // In a real implementation, this would use proper logging
                Console.WriteLine($"Error handling mouse input in plugin '{plugin.Id}': {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// Handles keyboard input for all enabled plugins.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="isPressed">Whether the key is pressed (true) or released (false).</param>
    /// <returns>True if any plugin handled the input, false otherwise.</returns>
    public bool HandleKeyboardInput(string key, bool isPressed)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        IScenePlugin[] enabledPlugins;

        lock (_lock)
        {
            enabledPlugins = _plugins.Values.Where(p => p.IsEnabled).ToArray();
        }

        // Allow plugins to handle input in registration order
        foreach (var plugin in enabledPlugins)
        {
            try
            {
                if (plugin.HandleKeyboardInput(key, isPressed))
                    return true; // Input was handled
            }
            catch (Exception ex)
            {
                // In a real implementation, this would use proper logging
                Console.WriteLine($"Error handling keyboard input in plugin '{plugin.Id}': {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// Shuts down all plugins and clears the registry.
    /// </summary>
    public Result ShutdownAll()
    {
        lock (_lock)
        {
            var errors = new List<string>();

            foreach (var plugin in _plugins.Values)
            {
                try
                {
                    var shutdownResult = plugin.Shutdown();
                    if (shutdownResult.IsFailure)
                    {
                        errors.Add($"Plugin '{plugin.Id}': {shutdownResult.Error}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Plugin '{plugin.Id}': {ex.Message}");
                }
            }

            _plugins.Clear();

            if (errors.Any())
            {
                return Result.Fail($"Some plugins failed to shutdown: {string.Join(", ", errors)}");
            }

            return Result.Ok();
        }
    }

    /// <summary>
    /// Gets plugin statistics.
    /// </summary>
    /// <returns>Dictionary containing plugin statistics.</returns>
    public Dictionary<string, object> GetStatistics()
    {
        lock (_lock)
        {
            var totalPlugins = _plugins.Count;
            var enabledPlugins = _plugins.Values.Count(p => p.IsEnabled);
            var configurablePlugins = _plugins.Values.Count(p => p.IsConfigurable);

            return new Dictionary<string, object>
            {
                ["TotalPlugins"] = totalPlugins,
                ["EnabledPlugins"] = enabledPlugins,
                ["DisabledPlugins"] = totalPlugins - enabledPlugins,
                ["ConfigurablePlugins"] = configurablePlugins,
                ["PluginIds"] = _plugins.Keys.ToArray()
            };
        }
    }

    /// <summary>
    /// Registers the default plugins provided by the infrastructure layer.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    public Result RegisterDefaultPlugins()
    {
        try
        {
            // Register the flight path plugin
            var flightPathPlugin = new FlightPathPlugin();
            var result = RegisterPlugin(flightPathPlugin);
            if (result.IsFailure)
                return result;

            // Future plugins would be registered here
            // var anotherPlugin = new AnotherPlugin();
            // RegisterPlugin(anotherPlugin);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to register default plugins: {ex.Message}");
        }
    }
}
