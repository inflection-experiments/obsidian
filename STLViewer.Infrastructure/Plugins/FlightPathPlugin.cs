using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Infrastructure.Plugins;

/// <summary>
/// Plugin for visualizing flight paths and managing waypoints in the 3D scene.
/// </summary>
public class FlightPathPlugin : IScenePlugin
{
    private readonly Dictionary<string, FlightPath> _flightPaths = new();
    private readonly Dictionary<string, object> _configuration = new();
    private bool _isEnabled = false;
    private bool _isInitialized = false;

    // Animation state
    private float _animationTime = 0.0f;
    private bool _isAnimating = false;
    private float _animationSpeed = 0.1f; // Units per second
    private string? _activeFlightPathId;

    // Visual settings
    private Vector3 _pathColor = new(0.0f, 1.0f, 0.0f); // Green
    private Vector3 _waypointColor = new(1.0f, 0.0f, 0.0f); // Red
    private Vector3 _activeWaypointColor = new(1.0f, 1.0f, 0.0f); // Yellow
    private float _pathLineWidth = 2.0f;
    private float _waypointSize = 0.5f;
    private bool _showWaypoints = true;
    private bool _showPath = true;
    private bool _showTrajectory = true;
    private int _trajectoryResolution = 100;

    public string Id => "flight-path-visualizer";
    public string Name => "Flight Path Visualizer";
    public string Description => "Visualizes flight paths with waypoint management and trajectory control for aircraft and other moving objects.";
    public string Version => "1.0.0";
    public bool IsEnabled => _isEnabled;
    public bool IsConfigurable => true;

    public Result Initialize()
    {
        try
        {
            // Initialize default configuration
            _configuration["PathColor"] = new float[] { 0.0f, 1.0f, 0.0f };
            _configuration["WaypointColor"] = new float[] { 1.0f, 0.0f, 0.0f };
            _configuration["ActiveWaypointColor"] = new float[] { 1.0f, 1.0f, 0.0f };
            _configuration["PathLineWidth"] = 2.0f;
            _configuration["WaypointSize"] = 0.5f;
            _configuration["ShowWaypoints"] = true;
            _configuration["ShowPath"] = true;
            _configuration["ShowTrajectory"] = true;
            _configuration["TrajectoryResolution"] = 100;
            _configuration["AnimationSpeed"] = 0.1f;

            _isInitialized = true;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to initialize FlightPathPlugin: {ex.Message}");
        }
    }

    public Result Shutdown()
    {
        try
        {
            _flightPaths.Clear();
            _configuration.Clear();
            _isEnabled = false;
            _isInitialized = false;
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to shutdown FlightPathPlugin: {ex.Message}");
        }
    }

    public Result Enable()
    {
        if (!_isInitialized)
            return Result.Fail("Plugin must be initialized before enabling");

        _isEnabled = true;
        return Result.Ok();
    }

    public Result Disable()
    {
        _isEnabled = false;
        _isAnimating = false;
        return Result.Ok();
    }

    public void Update(float deltaTime)
    {
        if (!_isEnabled || !_isAnimating || string.IsNullOrEmpty(_activeFlightPathId))
            return;

        if (!_flightPaths.TryGetValue(_activeFlightPathId, out var flightPath))
        {
            _isAnimating = false;
            return;
        }

        // Update animation time
        _animationTime += deltaTime * _animationSpeed;

        // Loop animation if it exceeds the path
        if (_animationTime > 1.0f)
        {
            if (flightPath.IsClosed)
            {
                _animationTime = _animationTime - 1.0f; // Seamless loop
            }
            else
            {
                _animationTime = 1.0f; // Stop at end
                _isAnimating = false;
            }
        }
    }

    public void Render(IRenderer renderer, ICamera camera)
    {
        if (!_isEnabled || renderer == null)
            return;

        foreach (var flightPath in _flightPaths.Values)
        {
            RenderFlightPath(renderer, flightPath);
        }
    }

    public Dictionary<string, object> GetConfiguration()
    {
        return new Dictionary<string, object>(_configuration);
    }

    public Result SetConfiguration(Dictionary<string, object> configuration)
    {
        try
        {
            foreach (var kvp in configuration)
            {
                _configuration[kvp.Key] = kvp.Value;
            }

            // Apply configuration values
            ApplyConfiguration();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to set configuration: {ex.Message}");
        }
    }

    public bool HandleMouseInput(float mouseX, float mouseY, bool isPressed, ICamera camera)
    {
        if (!_isEnabled)
            return false;

        // TODO: Implement waypoint selection and manipulation via mouse
        // This would involve ray-casting from mouse position to 3D space
        // and checking for intersections with waypoint spheres
        return false;
    }

    public bool HandleKeyboardInput(string key, bool isPressed)
    {
        if (!_isEnabled || !isPressed)
            return false;

        switch (key.ToLowerInvariant())
        {
            case "space":
                ToggleAnimation();
                return true;
            case "r":
                ResetAnimation();
                return true;
            case "p":
                _showPath = !_showPath;
                return true;
            case "w":
                _showWaypoints = !_showWaypoints;
                return true;
            case "t":
                _showTrajectory = !_showTrajectory;
                return true;
            default:
                return false;
        }
    }

    // Plugin-specific methods

    /// <summary>
    /// Adds a new flight path to the plugin.
    /// </summary>
    /// <param name="flightPath">The flight path to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result AddFlightPath(FlightPath flightPath)
    {
        if (flightPath == null)
            return Result.Fail("Flight path cannot be null");

        if (_flightPaths.ContainsKey(flightPath.Name))
            return Result.Fail($"Flight path with name '{flightPath.Name}' already exists");

        _flightPaths[flightPath.Name] = flightPath;
        return Result.Ok();
    }

    /// <summary>
    /// Removes a flight path from the plugin.
    /// </summary>
    /// <param name="name">The name of the flight path to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveFlightPath(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Result.Fail("Flight path name cannot be null or empty");

        if (!_flightPaths.ContainsKey(name))
            return Result.Fail($"Flight path '{name}' not found");

        _flightPaths.Remove(name);

        // Stop animation if this was the active path
        if (_activeFlightPathId == name)
        {
            _isAnimating = false;
            _activeFlightPathId = null;
        }

        return Result.Ok();
    }

    /// <summary>
    /// Gets a flight path by name.
    /// </summary>
    /// <param name="name">The name of the flight path.</param>
    /// <returns>The flight path if found, null otherwise.</returns>
    public FlightPath? GetFlightPath(string name)
    {
        return _flightPaths.TryGetValue(name, out var flightPath) ? flightPath : null;
    }

    /// <summary>
    /// Gets all flight paths.
    /// </summary>
    /// <returns>Collection of all flight paths.</returns>
    public IEnumerable<FlightPath> GetAllFlightPaths()
    {
        return _flightPaths.Values;
    }

    /// <summary>
    /// Starts animation for the specified flight path.
    /// </summary>
    /// <param name="flightPathName">The name of the flight path to animate.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result StartAnimation(string flightPathName)
    {
        if (!_flightPaths.ContainsKey(flightPathName))
            return Result.Fail($"Flight path '{flightPathName}' not found");

        _activeFlightPathId = flightPathName;
        _animationTime = 0.0f;
        _isAnimating = true;
        return Result.Ok();
    }

    /// <summary>
    /// Stops the current animation.
    /// </summary>
    public void StopAnimation()
    {
        _isAnimating = false;
    }

    /// <summary>
    /// Toggles animation playback.
    /// </summary>
    public void ToggleAnimation()
    {
        if (_isAnimating)
        {
            StopAnimation();
        }
        else if (!string.IsNullOrEmpty(_activeFlightPathId))
        {
            _isAnimating = true;
        }
    }

    /// <summary>
    /// Resets animation to the beginning.
    /// </summary>
    public void ResetAnimation()
    {
        _animationTime = 0.0f;
    }

    /// <summary>
    /// Sets the animation speed.
    /// </summary>
    /// <param name="speed">The animation speed factor.</param>
    public void SetAnimationSpeed(float speed)
    {
        _animationSpeed = MathF.Max(0.001f, speed);
        _configuration["AnimationSpeed"] = _animationSpeed;
    }

    /// <summary>
    /// Gets the current animation position for the active flight path.
    /// </summary>
    /// <returns>The current position along the path, or null if no active animation.</returns>
    public Vector3? GetCurrentAnimationPosition()
    {
        if (!_isAnimating || string.IsNullOrEmpty(_activeFlightPathId))
            return null;

        if (!_flightPaths.TryGetValue(_activeFlightPathId, out var flightPath))
            return null;

        return flightPath.GetPositionAtTime(_animationTime);
    }

    /// <summary>
    /// Creates a sample flight path for demonstration purposes.
    /// </summary>
    /// <returns>A sample flight path with waypoints around the fighter plane model.</returns>
    public static FlightPath CreateSampleFlightPath()
    {
        var flightPath = FlightPath.Create("Sample Flight Path", "Demonstration flight path around a fighter plane model");

        // Create waypoints in a pattern around the fighter plane
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(-15, 5, 10), 20.0f, "Approach"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(-10, 3, 5), 15.0f, "Turn 1"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(0, 2, 0), 25.0f, "Flyby"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(10, 4, -5), 20.0f, "Turn 2"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(15, 8, -10), 18.0f, "Climb"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(10, 12, -5), 22.0f, "High Pass"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(-5, 10, 5), 20.0f, "Return"));
        flightPath.AddWaypoint(Waypoint.CreateWithSpeed(new Vector3(-15, 6, 10), 15.0f, "Final"));

        flightPath.SetClosed(true);
        flightPath.SetInterpolationMethod(TrajectoryInterpolation.Smooth);

        return flightPath;
    }

    private void RenderFlightPath(IRenderer renderer, FlightPath flightPath)
    {
        // Render trajectory path
        if (_showTrajectory && _showPath && flightPath.Waypoints.Count > 1)
        {
            var trajectory = flightPath.CalculateTrajectory(_trajectoryResolution);
            RenderTrajectoryLines(renderer, trajectory);
        }

        // Render waypoints
        if (_showWaypoints)
        {
            for (int i = 0; i < flightPath.Waypoints.Count; i++)
            {
                var waypoint = flightPath.Waypoints[i];
                RenderWaypoint(renderer, waypoint, i);
            }
        }

        // Render current animation position
        if (_isAnimating && _activeFlightPathId == flightPath.Name)
        {
            var currentPos = flightPath.GetPositionAtTime(_animationTime);
            RenderAnimationMarker(renderer, currentPos);
        }
    }

    private void RenderTrajectoryLines(IRenderer renderer, Vector3[] trajectory)
    {
        // This is a placeholder - actual implementation would depend on the renderer interface
        // In a real implementation, this would create line primitives for the renderer
        // renderer.DrawLines(trajectory, _pathColor, _pathLineWidth);
    }

    private void RenderWaypoint(IRenderer renderer, Waypoint waypoint, int index)
    {
        // This is a placeholder - actual implementation would depend on the renderer interface
        // In a real implementation, this would create sphere primitives at waypoint positions
        // renderer.DrawSphere(waypoint.Position, _waypointSize, _waypointColor);

        // Could also render waypoint labels
        // if (!string.IsNullOrEmpty(waypoint.Name))
        //     renderer.DrawText(waypoint.Position + Vector3.UnitY * _waypointSize * 2, waypoint.Name);
    }

    private void RenderAnimationMarker(IRenderer renderer, Vector3 position)
    {
        // This is a placeholder - actual implementation would depend on the renderer interface
        // renderer.DrawSphere(position, _waypointSize * 1.5f, _activeWaypointColor);
    }

    private void ApplyConfiguration()
    {
        if (_configuration.TryGetValue("PathColor", out var pathColor) && pathColor is float[] pc && pc.Length >= 3)
            _pathColor = new Vector3(pc[0], pc[1], pc[2]);

        if (_configuration.TryGetValue("WaypointColor", out var waypointColor) && waypointColor is float[] wc && wc.Length >= 3)
            _waypointColor = new Vector3(wc[0], wc[1], wc[2]);

        if (_configuration.TryGetValue("ActiveWaypointColor", out var activeColor) && activeColor is float[] ac && ac.Length >= 3)
            _activeWaypointColor = new Vector3(ac[0], ac[1], ac[2]);

        if (_configuration.TryGetValue("PathLineWidth", out var lineWidth) && lineWidth is float lw)
            _pathLineWidth = lw;

        if (_configuration.TryGetValue("WaypointSize", out var size) && size is float ws)
            _waypointSize = ws;

        if (_configuration.TryGetValue("ShowWaypoints", out var showWP) && showWP is bool swp)
            _showWaypoints = swp;

        if (_configuration.TryGetValue("ShowPath", out var showP) && showP is bool sp)
            _showPath = sp;

        if (_configuration.TryGetValue("ShowTrajectory", out var showT) && showT is bool st)
            _showTrajectory = st;

        if (_configuration.TryGetValue("TrajectoryResolution", out var resolution) && resolution is int tr)
            _trajectoryResolution = tr;

        if (_configuration.TryGetValue("AnimationSpeed", out var animSpeed) && animSpeed is float aSpeed)
            _animationSpeed = aSpeed;
    }
}
