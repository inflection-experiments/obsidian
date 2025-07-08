using STLViewer.Domain.Enums;
using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Base record for all measurement types.
/// </summary>
public abstract record Measurement
{
    /// <summary>
    /// Gets the type of measurement.
    /// </summary>
    public abstract MeasurementType Type { get; }

    /// <summary>
    /// Gets the timestamp when the measurement was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets a human-readable description of the measurement.
    /// </summary>
    public abstract string Description { get; }

    /// <summary>
    /// Gets the formatted string representation of the measurement value.
    /// </summary>
    public abstract string FormattedValue { get; }
}

/// <summary>
/// Represents a distance measurement between two points in 3D space.
/// </summary>
public sealed record DistanceMeasurement : Measurement
{
    /// <summary>
    /// The first point of the measurement.
    /// </summary>
    public Vector3 Point1 { get; init; }

    /// <summary>
    /// The second point of the measurement.
    /// </summary>
    public Vector3 Point2 { get; init; }

    /// <summary>
    /// The calculated distance in model units.
    /// </summary>
    public float Distance { get; init; }

    /// <summary>
    /// The unit of measurement (e.g., "mm", "inches").
    /// </summary>
    public string Unit { get; init; } = "units";

    public override MeasurementType Type => MeasurementType.Distance;

    public override string Description => $"Distance from {Point1} to {Point2}";

    public override string FormattedValue => $"{Distance:F3} {Unit}";

    /// <summary>
    /// Creates a distance measurement between two points.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <returns>A new distance measurement.</returns>
    public static DistanceMeasurement Create(Vector3 point1, Vector3 point2, string unit = "units")
    {
        var distance = Vector3.Distance(point1, point2);
        return new DistanceMeasurement
        {
            Point1 = point1,
            Point2 = point2,
            Distance = distance,
            Unit = unit
        };
    }
}

/// <summary>
/// Represents an angle measurement between vectors or points.
/// </summary>
public sealed record AngleMeasurement : Measurement
{
    /// <summary>
    /// The vertex point of the angle (where the two rays meet).
    /// </summary>
    public Vector3 Vertex { get; init; }

    /// <summary>
    /// The first point defining one ray of the angle.
    /// </summary>
    public Vector3 Point1 { get; init; }

    /// <summary>
    /// The second point defining the other ray of the angle.
    /// </summary>
    public Vector3 Point2 { get; init; }

    /// <summary>
    /// The calculated angle in radians.
    /// </summary>
    public float AngleRadians { get; init; }

    /// <summary>
    /// The calculated angle in degrees.
    /// </summary>
    public float AngleDegrees => AngleRadians * 180.0f / MathF.PI;

    public override MeasurementType Type => MeasurementType.Angle;

    public override string Description => $"Angle at {Vertex} between {Point1} and {Point2}";

    public override string FormattedValue => $"{AngleDegrees:F1}°";

    /// <summary>
    /// Creates an angle measurement between three points.
    /// </summary>
    /// <param name="vertex">The vertex point of the angle.</param>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <returns>A new angle measurement.</returns>
    public static AngleMeasurement Create(Vector3 vertex, Vector3 point1, Vector3 point2)
    {
        var vector1 = (point1 - vertex).Normalized();
        var vector2 = (point2 - vertex).Normalized();
        var dot = Vector3.Dot(vector1, vector2);

        // Clamp to avoid numerical errors
        dot = MathF.Max(-1.0f, MathF.Min(1.0f, dot));
        var angleRadians = MathF.Acos(dot);

        return new AngleMeasurement
        {
            Vertex = vertex,
            Point1 = point1,
            Point2 = point2,
            AngleRadians = angleRadians
        };
    }
}

/// <summary>
/// Represents a volume measurement of a 3D model.
/// </summary>
public sealed record VolumeMeasurement : Measurement
{
    /// <summary>
    /// The calculated volume in cubic units.
    /// </summary>
    public float Volume { get; init; }

    /// <summary>
    /// The unit of measurement (e.g., "mm³", "cubic inches").
    /// </summary>
    public string Unit { get; init; } = "cubic units";

    /// <summary>
    /// Whether the model is a closed mesh suitable for volume calculation.
    /// </summary>
    public bool IsClosedMesh { get; init; }

    /// <summary>
    /// The accuracy confidence level of the volume calculation.
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    public override MeasurementType Type => MeasurementType.Volume;

    public override string Description => IsClosedMesh
        ? "Volume of closed mesh"
        : "Estimated volume (mesh may not be closed)";

    public override string FormattedValue => $"{Volume:F3} {Unit}" +
        (Confidence < 1.0f ? $" (±{(1.0f - Confidence) * 100:F1}%)" : "");
}

/// <summary>
/// Represents a surface area measurement of a 3D model.
/// </summary>
public sealed record SurfaceAreaMeasurement : Measurement
{
    /// <summary>
    /// The calculated surface area in square units.
    /// </summary>
    public float SurfaceArea { get; init; }

    /// <summary>
    /// The unit of measurement (e.g., "mm²", "square inches").
    /// </summary>
    public string Unit { get; init; } = "square units";

    /// <summary>
    /// The number of triangles used in the calculation.
    /// </summary>
    public int TriangleCount { get; init; }

    public override MeasurementType Type => MeasurementType.SurfaceArea;

    public override string Description => $"Surface area calculated from {TriangleCount} triangles";

    public override string FormattedValue => $"{SurfaceArea:F3} {Unit}";
}

/// <summary>
/// Represents bounding box measurements and properties.
/// </summary>
public sealed record BoundingBoxMeasurement : Measurement
{
    /// <summary>
    /// The bounding box of the model.
    /// </summary>
    public BoundingBox BoundingBox { get; init; }

    /// <summary>
    /// The unit of measurement for dimensions.
    /// </summary>
    public string Unit { get; init; } = "units";

    /// <summary>
    /// The width (X dimension) of the bounding box.
    /// </summary>
    public float Width => BoundingBox.Size.X;

    /// <summary>
    /// The height (Y dimension) of the bounding box.
    /// </summary>
    public float Height => BoundingBox.Size.Y;

    /// <summary>
    /// The depth (Z dimension) of the bounding box.
    /// </summary>
    public float Depth => BoundingBox.Size.Z;

    /// <summary>
    /// The volume of the bounding box.
    /// </summary>
    public float BoundingVolume => Width * Height * Depth;

    /// <summary>
    /// The center point of the bounding box.
    /// </summary>
    public Vector3 Center => BoundingBox.Center;

    public override MeasurementType Type => MeasurementType.BoundingBox;

    public override string Description => "Model bounding box dimensions";

    public override string FormattedValue =>
        $"{Width:F3} × {Height:F3} × {Depth:F3} {Unit}";

    /// <summary>
    /// Gets detailed bounding box information as a formatted string.
    /// </summary>
    public string DetailedInfo =>
        $"Width: {Width:F3} {Unit}\n" +
        $"Height: {Height:F3} {Unit}\n" +
        $"Depth: {Depth:F3} {Unit}\n" +
        $"Volume: {BoundingVolume:F3} cubic {Unit}\n" +
        $"Center: ({Center.X:F3}, {Center.Y:F3}, {Center.Z:F3})";
}

/// <summary>
/// Represents a centroid/center of mass measurement.
/// </summary>
public sealed record CentroidMeasurement : Measurement
{
    /// <summary>
    /// The calculated centroid position.
    /// </summary>
    public Vector3 Centroid { get; init; }

    /// <summary>
    /// The method used to calculate the centroid.
    /// </summary>
    public string CalculationMethod { get; init; } = "Geometric";

    /// <summary>
    /// The unit of measurement for coordinates.
    /// </summary>
    public string Unit { get; init; } = "units";

    public override MeasurementType Type => MeasurementType.Centroid;

    public override string Description => $"{CalculationMethod} centroid";

    public override string FormattedValue =>
        $"({Centroid.X:F3}, {Centroid.Y:F3}, {Centroid.Z:F3}) {Unit}";
}
