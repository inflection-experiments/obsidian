using STLViewer.Domain.Enums;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents a collection of measurements taken during a measurement session.
/// </summary>
public sealed record MeasurementSession
{
    /// <summary>
    /// Gets the unique identifier for this measurement session.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the name of the measurement session.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when the session was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when the session was last modified.
    /// </summary>
    public DateTime LastModified { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the read-only collection of measurements in this session.
    /// </summary>
    public IReadOnlyList<Measurement> Measurements { get; init; } = new List<Measurement>();

    /// <summary>
    /// Gets the total number of measurements in this session.
    /// </summary>
    public int Count => Measurements.Count;

    /// <summary>
    /// Gets a value indicating whether this session has any measurements.
    /// </summary>
    public bool HasMeasurements => Measurements.Count > 0;

    /// <summary>
    /// Gets measurements grouped by type.
    /// </summary>
    public IReadOnlyDictionary<MeasurementType, IReadOnlyList<Measurement>> MeasurementsByType =>
        Measurements
            .GroupBy(m => m.Type)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Measurement>)g.ToList());

    /// <summary>
    /// Gets a summary of measurement counts by type.
    /// </summary>
    public IReadOnlyDictionary<MeasurementType, int> CountsByType =>
        Measurements
            .GroupBy(m => m.Type)
            .ToDictionary(g => g.Key, g => g.Count());



        /// <summary>
    /// Creates a new measurement session.
    /// </summary>
    /// <param name="name">The name of the session.</param>
    /// <param name="measurements">Optional initial measurements.</param>
    /// <returns>A new measurement session.</returns>
    public static MeasurementSession Create(string name, IEnumerable<Measurement>? measurements = null)
    {
        var measurementList = measurements?.ToList() ?? new List<Measurement>();

        return new MeasurementSession
        {
            Name = name,
            Measurements = measurementList,
            LastModified = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new session with a default name based on the current timestamp.
    /// </summary>
    /// <returns>A new measurement session with a timestamped name.</returns>
    public static MeasurementSession CreateDefault()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return Create($"Measurement Session {timestamp}");
    }

        /// <summary>
    /// Adds a measurement to this session.
    /// </summary>
    /// <param name="measurement">The measurement to add.</param>
    /// <returns>A new session with the added measurement.</returns>
    public MeasurementSession AddMeasurement(Measurement measurement)
    {
        if (measurement == null)
            throw new ArgumentNullException(nameof(measurement));

        var newMeasurements = new List<Measurement>(Measurements) { measurement };
        return this with
        {
            Measurements = newMeasurements,
            LastModified = DateTime.UtcNow
        };
    }

        /// <summary>
    /// Adds multiple measurements to this session.
    /// </summary>
    /// <param name="measurements">The measurements to add.</param>
    /// <returns>A new session with the added measurements.</returns>
    public MeasurementSession AddMeasurements(IEnumerable<Measurement> measurements)
    {
        if (measurements == null)
            throw new ArgumentNullException(nameof(measurements));

        var measurementList = measurements.ToList();
        if (measurementList.Count == 0)
            return this;

        var newMeasurements = new List<Measurement>(Measurements);
        newMeasurements.AddRange(measurementList);

        return this with
        {
            Measurements = newMeasurements,
            LastModified = DateTime.UtcNow
        };
    }

        /// <summary>
    /// Removes a measurement from this session by index.
    /// </summary>
    /// <param name="index">The index of the measurement to remove.</param>
    /// <returns>A new session with the measurement removed.</returns>
    public MeasurementSession RemoveMeasurement(int index)
    {
        if (index < 0 || index >= Measurements.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var newMeasurements = new List<Measurement>(Measurements);
        newMeasurements.RemoveAt(index);

        return this with
        {
            Measurements = newMeasurements,
            LastModified = DateTime.UtcNow
        };
    }

        /// <summary>
    /// Removes all measurements of a specific type.
    /// </summary>
    /// <param name="type">The type of measurements to remove.</param>
    /// <returns>A new session with the measurements removed.</returns>
    public MeasurementSession RemoveMeasurementsByType(MeasurementType type)
    {
        var newMeasurements = Measurements.Where(m => m.Type != type).ToList();

        if (newMeasurements.Count == Measurements.Count)
            return this; // No changes

        return this with
        {
            Measurements = newMeasurements,
            LastModified = DateTime.UtcNow
        };
    }

        /// <summary>
    /// Clears all measurements from this session.
    /// </summary>
    /// <returns>A new empty session.</returns>
    public MeasurementSession Clear()
    {
        if (Measurements.Count == 0)
            return this;

        return this with
        {
            Measurements = new List<Measurement>(),
            LastModified = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Renames this measurement session.
    /// </summary>
    /// <param name="newName">The new name for the session.</param>
    /// <returns>A new session with the updated name.</returns>
    public MeasurementSession Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Session name cannot be null or empty.", nameof(newName));

        if (Name == newName)
            return this;

        return this with
        {
            Name = newName,
            LastModified = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets measurements of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of measurement to retrieve.</typeparam>
    /// <returns>Measurements of the specified type.</returns>
    public IEnumerable<T> GetMeasurements<T>() where T : Measurement
    {
        return Measurements.OfType<T>();
    }

    /// <summary>
    /// Gets measurements of a specific measurement type.
    /// </summary>
    /// <param name="type">The measurement type to filter by.</param>
    /// <returns>Measurements of the specified type.</returns>
    public IEnumerable<Measurement> GetMeasurements(MeasurementType type)
    {
        return Measurements.Where(m => m.Type == type);
    }

    /// <summary>
    /// Gets a summary of this measurement session.
    /// </summary>
    public string Summary
    {
        get
        {
            if (!HasMeasurements)
                return "No measurements";

            var typeCount = CountsByType;
            var summaryParts = typeCount.Select(kvp => $"{kvp.Value} {kvp.Key}").ToList();

            return string.Join(", ", summaryParts);
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Summary}) - Modified: {LastModified:yyyy-MM-dd HH:mm:ss}";
    }
}
