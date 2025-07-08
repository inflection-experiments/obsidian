using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents a waypoint in a 3D flight path.
/// </summary>
public class Waypoint
{
    /// <summary>
    /// Gets the unique identifier for this waypoint.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the 3D position of this waypoint.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the optional name or label for this waypoint.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the altitude above ground level (optional).
    /// </summary>
    public float? Altitude { get; }

    /// <summary>
    /// Gets the desired speed at this waypoint in units/second (optional).
    /// </summary>
    public float? Speed { get; }

    /// <summary>
    /// Gets the desired heading/yaw angle in degrees (optional).
    /// </summary>
    public float? Heading { get; }

    /// <summary>
    /// Gets the desired pitch angle in degrees (optional).
    /// </summary>
    public float? Pitch { get; }

    /// <summary>
    /// Gets the desired roll angle in degrees (optional).
    /// </summary>
    public float? Roll { get; }

    /// <summary>
    /// Gets the dwell time at this waypoint in seconds (optional).
    /// </summary>
    public float? DwellTime { get; }

    /// <summary>
    /// Gets additional metadata for this waypoint.
    /// </summary>
    public Dictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Initializes a new instance of the Waypoint class.
    /// </summary>
    /// <param name="position">The 3D position of the waypoint.</param>
    /// <param name="name">Optional name or label.</param>
    /// <param name="altitude">Optional altitude above ground level.</param>
    /// <param name="speed">Optional desired speed.</param>
    /// <param name="heading">Optional heading angle in degrees.</param>
    /// <param name="pitch">Optional pitch angle in degrees.</param>
    /// <param name="roll">Optional roll angle in degrees.</param>
    /// <param name="dwellTime">Optional dwell time in seconds.</param>
    /// <param name="metadata">Optional additional metadata.</param>
    public Waypoint(
        Vector3 position,
        string? name = null,
        float? altitude = null,
        float? speed = null,
        float? heading = null,
        float? pitch = null,
        float? roll = null,
        float? dwellTime = null,
        Dictionary<string, object>? metadata = null)
    {
        Id = Guid.NewGuid();
        Position = position;
        Name = name;
        Altitude = altitude;
        Speed = speed;
        Heading = heading;
        Pitch = pitch;
        Roll = roll;
        DwellTime = dwellTime;
        Metadata = metadata ?? new Dictionary<string, object>();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a simple waypoint with just a position.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="name">Optional name.</param>
    /// <returns>A new waypoint.</returns>
    public static Waypoint Create(float x, float y, float z, string? name = null)
    {
        return new Waypoint(new Vector3(x, y, z), name);
    }

    /// <summary>
    /// Creates a waypoint with position and speed.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="speed">The desired speed.</param>
    /// <param name="name">Optional name.</param>
    /// <returns>A new waypoint.</returns>
    public static Waypoint CreateWithSpeed(Vector3 position, float speed, string? name = null)
    {
        return new Waypoint(position, name, speed: speed);
    }

    /// <summary>
    /// Creates a waypoint with full orientation data.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="heading">The heading angle in degrees.</param>
    /// <param name="pitch">The pitch angle in degrees.</param>
    /// <param name="roll">The roll angle in degrees.</param>
    /// <param name="name">Optional name.</param>
    /// <returns>A new waypoint.</returns>
    public static Waypoint CreateWithOrientation(
        Vector3 position,
        float heading,
        float pitch,
        float roll,
        string? name = null)
    {
        return new Waypoint(position, name, heading: heading, pitch: pitch, roll: roll);
    }

    /// <summary>
    /// Calculates the distance to another waypoint.
    /// </summary>
    /// <param name="other">The other waypoint.</param>
    /// <returns>The distance between waypoints.</returns>
    public float DistanceTo(Waypoint other)
    {
        return Vector3.Distance(Position, other.Position);
    }

    /// <summary>
    /// Calculates the direction vector to another waypoint.
    /// </summary>
    /// <param name="other">The other waypoint.</param>
    /// <returns>The normalized direction vector.</returns>
    public Vector3 DirectionTo(Waypoint other)
    {
        return (other.Position - Position).Normalized();
    }

    /// <summary>
    /// Creates a copy of this waypoint with a new position.
    /// </summary>
    /// <param name="newPosition">The new position.</param>
    /// <returns>A new waypoint with the updated position.</returns>
    public Waypoint WithPosition(Vector3 newPosition)
    {
        return new Waypoint(
            newPosition, Name, Altitude, Speed,
            Heading, Pitch, Roll, DwellTime,
            new Dictionary<string, object>(Metadata));
    }

    /// <summary>
    /// Creates a copy of this waypoint with a new name.
    /// </summary>
    /// <param name="newName">The new name.</param>
    /// <returns>A new waypoint with the updated name.</returns>
    public Waypoint WithName(string? newName)
    {
        return new Waypoint(
            Position, newName, Altitude, Speed,
            Heading, Pitch, Roll, DwellTime,
            new Dictionary<string, object>(Metadata));
    }

    /// <summary>
    /// Creates a copy of this waypoint with new speed.
    /// </summary>
    /// <param name="newSpeed">The new speed.</param>
    /// <returns>A new waypoint with the updated speed.</returns>
    public Waypoint WithSpeed(float? newSpeed)
    {
        return new Waypoint(
            Position, Name, Altitude, newSpeed,
            Heading, Pitch, Roll, DwellTime,
            new Dictionary<string, object>(Metadata));
    }

    public override bool Equals(object? obj)
    {
        return obj is Waypoint other && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        var name = !string.IsNullOrEmpty(Name) ? $" '{Name}'" : "";
        return $"Waypoint{name} at {Position}";
    }
}
