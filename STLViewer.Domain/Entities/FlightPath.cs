using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Domain.Entities;

/// <summary>
/// Represents a flight path consisting of multiple waypoints and trajectory calculations.
/// </summary>
public class FlightPath : Entity<Guid>
{
    private readonly List<Waypoint> _waypoints = new();
    private readonly Dictionary<string, object> _properties = new();

    /// <summary>
    /// Gets the name of this flight path.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the description of this flight path.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the waypoints in this flight path.
    /// </summary>
    public IReadOnlyList<Waypoint> Waypoints => _waypoints.AsReadOnly();

    /// <summary>
    /// Gets the total length of the flight path.
    /// </summary>
    public float TotalLength => CalculateTotalLength();

    /// <summary>
    /// Gets the estimated flight time in seconds.
    /// </summary>
    public float EstimatedFlightTime => CalculateEstimatedFlightTime();

    /// <summary>
    /// Gets the bounding box of all waypoints.
    /// </summary>
    public BoundingBox BoundingBox => CalculateBoundingBox();

    /// <summary>
    /// Gets whether this flight path is closed (first and last waypoints are connected).
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// Gets the interpolation method used for trajectory calculation.
    /// </summary>
    public TrajectoryInterpolation InterpolationMethod { get; private set; }

    /// <summary>
    /// Gets additional properties for this flight path.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties => _properties.AsReadOnly();

        /// <summary>
    /// Initializes a new instance of the FlightPath class.
    /// </summary>
    /// <param name="name">The name of the flight path.</param>
    /// <param name="description">The description of the flight path.</param>
    /// <param name="interpolationMethod">The interpolation method to use.</param>
    public FlightPath(
        string name,
        string description = "",
        TrajectoryInterpolation interpolationMethod = TrajectoryInterpolation.Linear)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        InterpolationMethod = interpolationMethod;
    }

    /// <summary>
    /// Creates a new flight path with the specified name.
    /// </summary>
    /// <param name="name">The name of the flight path.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A new flight path.</returns>
    public static FlightPath Create(string name, string description = "")
    {
        return new FlightPath(name, description);
    }

    /// <summary>
    /// Adds a waypoint to the end of the flight path.
    /// </summary>
    /// <param name="waypoint">The waypoint to add.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result AddWaypoint(Waypoint waypoint)
    {
        if (waypoint == null)
            return Result.Fail("Waypoint cannot be null");

        if (_waypoints.Any(w => w.Id == waypoint.Id))
            return Result.Fail("Waypoint with this ID already exists");

        _waypoints.Add(waypoint);
        MarkAsUpdated();
        return Result.Ok();
    }

    /// <summary>
    /// Inserts a waypoint at the specified index.
    /// </summary>
    /// <param name="index">The index to insert at.</param>
    /// <param name="waypoint">The waypoint to insert.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result InsertWaypoint(int index, Waypoint waypoint)
    {
        if (waypoint == null)
            return Result.Fail("Waypoint cannot be null");

        if (index < 0 || index > _waypoints.Count)
            return Result.Fail("Index out of range");

        if (_waypoints.Any(w => w.Id == waypoint.Id))
            return Result.Fail("Waypoint with this ID already exists");

        _waypoints.Insert(index, waypoint);
        MarkAsUpdated();
        return Result.Ok();
    }

    /// <summary>
    /// Removes a waypoint by its ID.
    /// </summary>
    /// <param name="waypointId">The ID of the waypoint to remove.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result RemoveWaypoint(Guid waypointId)
    {
        var waypoint = _waypoints.FirstOrDefault(w => w.Id == waypointId);
        if (waypoint == null)
            return Result.Fail("Waypoint not found");

        _waypoints.Remove(waypoint);
        MarkAsUpdated();
        return Result.Ok();
    }

    /// <summary>
    /// Updates a waypoint in the flight path.
    /// </summary>
    /// <param name="waypointId">The ID of the waypoint to update.</param>
    /// <param name="updatedWaypoint">The updated waypoint.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result UpdateWaypoint(Guid waypointId, Waypoint updatedWaypoint)
    {
        if (updatedWaypoint == null)
            return Result.Fail("Updated waypoint cannot be null");

        var index = _waypoints.FindIndex(w => w.Id == waypointId);
        if (index == -1)
            return Result.Fail("Waypoint not found");

        _waypoints[index] = updatedWaypoint;
        MarkAsUpdated();
        return Result.Ok();
    }

    /// <summary>
    /// Clears all waypoints from the flight path.
    /// </summary>
    public void ClearWaypoints()
    {
        _waypoints.Clear();
        MarkAsUpdated();
    }

    /// <summary>
    /// Sets whether the flight path is closed.
    /// </summary>
    /// <param name="closed">True to close the path, false to open it.</param>
    public void SetClosed(bool closed)
    {
        IsClosed = closed;
        MarkAsUpdated();
    }

    /// <summary>
    /// Sets the interpolation method for trajectory calculation.
    /// </summary>
    /// <param name="method">The interpolation method.</param>
    public void SetInterpolationMethod(TrajectoryInterpolation method)
    {
        InterpolationMethod = method;
        MarkAsUpdated();
    }

    /// <summary>
    /// Calculates a position along the flight path at the specified time.
    /// </summary>
    /// <param name="time">The time parameter (0.0 to 1.0 for complete path).</param>
    /// <returns>The interpolated position.</returns>
    public Vector3 GetPositionAtTime(float time)
    {
        if (_waypoints.Count == 0)
            return Vector3.Zero;

        if (_waypoints.Count == 1)
            return _waypoints[0].Position;

        time = MathF.Max(0, MathF.Min(1, time));

        return InterpolationMethod switch
        {
            TrajectoryInterpolation.Linear => InterpolateLinear(time),
            TrajectoryInterpolation.Smooth => InterpolateSmooth(time),
            TrajectoryInterpolation.Bezier => InterpolateBezier(time),
            _ => InterpolateLinear(time)
        };
    }

    /// <summary>
    /// Calculates the trajectory points for the entire flight path.
    /// </summary>
    /// <param name="resolution">The number of points to calculate (default 100).</param>
    /// <returns>An array of trajectory points.</returns>
    public Vector3[] CalculateTrajectory(int resolution = 100)
    {
        if (_waypoints.Count == 0)
            return Array.Empty<Vector3>();

        if (_waypoints.Count == 1)
            return new[] { _waypoints[0].Position };

        var points = new Vector3[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            points[i] = GetPositionAtTime(t);
        }

        return points;
    }

    /// <summary>
    /// Sets a custom property for this flight path.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    public void SetProperty(string key, object value)
    {
        _properties[key] = value;
        MarkAsUpdated();
    }

    /// <summary>
    /// Gets a custom property value.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The property value or default.</returns>
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (_properties.TryGetValue(key, out var value) && value is T typedValue)
            return typedValue;
        return defaultValue;
    }

    private float CalculateTotalLength()
    {
        if (_waypoints.Count < 2)
            return 0;

        float totalLength = 0;
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            totalLength += _waypoints[i].DistanceTo(_waypoints[i + 1]);
        }

        if (IsClosed && _waypoints.Count > 2)
        {
            totalLength += _waypoints[^1].DistanceTo(_waypoints[0]);
        }

        return totalLength;
    }

    private float CalculateEstimatedFlightTime()
    {
        if (_waypoints.Count < 2)
            return 0;

        float totalTime = 0;
        for (int i = 0; i < _waypoints.Count - 1; i++)
        {
            var distance = _waypoints[i].DistanceTo(_waypoints[i + 1]);
            var speed = _waypoints[i].Speed ?? 10.0f; // Default speed
            totalTime += distance / speed;

            // Add dwell time
            var dwellTime = _waypoints[i].DwellTime;
            if (dwellTime.HasValue)
                totalTime += dwellTime.Value;
        }

        return totalTime;
    }

    private BoundingBox CalculateBoundingBox()
    {
        if (_waypoints.Count == 0)
            return new BoundingBox(Vector3.Zero, Vector3.Zero);

        var positions = _waypoints.Select(w => w.Position);
        return BoundingBox.FromPoints(positions);
    }

    private Vector3 InterpolateLinear(float time)
    {
        var segmentCount = IsClosed ? _waypoints.Count : _waypoints.Count - 1;
        var segmentTime = time * segmentCount;
        var segmentIndex = (int)segmentTime;
        var localTime = segmentTime - segmentIndex;

        if (segmentIndex >= segmentCount)
        {
            segmentIndex = segmentCount - 1;
            localTime = 1.0f;
        }

        var startWaypoint = _waypoints[segmentIndex];
        var endWaypoint = _waypoints[(segmentIndex + 1) % _waypoints.Count];

        return Vector3.Lerp(startWaypoint.Position, endWaypoint.Position, localTime);
    }

    private Vector3 InterpolateSmooth(float time)
    {
        // Use smoothstep for smoother interpolation
        var segmentCount = IsClosed ? _waypoints.Count : _waypoints.Count - 1;
        var segmentTime = time * segmentCount;
        var segmentIndex = (int)segmentTime;
        var localTime = segmentTime - segmentIndex;

        if (segmentIndex >= segmentCount)
        {
            segmentIndex = segmentCount - 1;
            localTime = 1.0f;
        }

        // Apply smoothstep function
        localTime = localTime * localTime * (3.0f - 2.0f * localTime);

        var startWaypoint = _waypoints[segmentIndex];
        var endWaypoint = _waypoints[(segmentIndex + 1) % _waypoints.Count];

        return Vector3.Lerp(startWaypoint.Position, endWaypoint.Position, localTime);
    }

    private Vector3 InterpolateBezier(float time)
    {
        // Simple quadratic Bezier interpolation
        // For more complex curves, this could be expanded to use cubic Bezier or B-splines
        if (_waypoints.Count < 3)
            return InterpolateLinear(time);

        var segmentCount = IsClosed ? _waypoints.Count : _waypoints.Count - 2;
        var segmentTime = time * segmentCount;
        var segmentIndex = (int)segmentTime;
        var localTime = segmentTime - segmentIndex;

        if (segmentIndex >= segmentCount)
        {
            segmentIndex = segmentCount - 1;
            localTime = 1.0f;
        }

        var p0 = _waypoints[segmentIndex].Position;
        var p1 = _waypoints[(segmentIndex + 1) % _waypoints.Count].Position;
        var p2 = _waypoints[(segmentIndex + 2) % _waypoints.Count].Position;

        // Quadratic Bezier formula: (1-t)²P0 + 2(1-t)tP1 + t²P2
        var oneMinusT = 1.0f - localTime;
        var result = oneMinusT * oneMinusT * p0 +
                     2 * oneMinusT * localTime * p1 +
                     localTime * localTime * p2;

        return result;
    }

    public override string ToString()
    {
        return $"FlightPath '{Name}' ({_waypoints.Count} waypoints, {TotalLength:F2} units)";
    }
}

/// <summary>
/// Enumeration of trajectory interpolation methods.
/// </summary>
public enum TrajectoryInterpolation
{
    /// <summary>
    /// Linear interpolation between waypoints.
    /// </summary>
    Linear,

    /// <summary>
    /// Smooth interpolation using smoothstep function.
    /// </summary>
    Smooth,

    /// <summary>
    /// Bezier curve interpolation.
    /// </summary>
    Bezier
}
