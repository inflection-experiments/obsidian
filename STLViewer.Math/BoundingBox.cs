namespace STLViewer.Math;

/// <summary>
/// Represents an axis-aligned bounding box in 3D space.
/// </summary>
public readonly struct BoundingBox : IEquatable<BoundingBox>
{
    /// <summary>
    /// The minimum corner of the bounding box.
    /// </summary>
    public readonly Vector3 Min;

    /// <summary>
    /// The maximum corner of the bounding box.
    /// </summary>
    public readonly Vector3 Max;

    /// <summary>
    /// Gets an empty bounding box.
    /// </summary>
    public static BoundingBox Empty => new(Vector3.Zero, Vector3.Zero);

    /// <summary>
    /// Initializes a new instance of the BoundingBox struct.
    /// </summary>
    /// <param name="min">The minimum corner.</param>
    /// <param name="max">The maximum corner.</param>
    public BoundingBox(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates a bounding box from a center point and size.
    /// </summary>
    /// <param name="center">The center point.</param>
    /// <param name="size">The size in each dimension.</param>
    /// <returns>A new bounding box.</returns>
    public static BoundingBox FromCenterAndSize(Vector3 center, Vector3 size)
    {
        var halfSize = size * 0.5f;
        return new BoundingBox(center - halfSize, center + halfSize);
    }

    /// <summary>
    /// Creates a bounding box that encompasses all the given points.
    /// </summary>
    /// <param name="points">The points to encompass.</param>
    /// <returns>A new bounding box.</returns>
    /// <exception cref="ArgumentException">Thrown when no points are provided.</exception>
    public static BoundingBox FromPoints(IEnumerable<Vector3> points)
    {
        var pointList = points.ToList();
        if (!pointList.Any())
            throw new ArgumentException("At least one point is required.", nameof(points));

        var min = pointList[0];
        var max = pointList[0];

        foreach (var point in pointList.Skip(1))
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        return new BoundingBox(min, max);
    }

    /// <summary>
    /// Gets the center point of the bounding box.
    /// </summary>
    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Gets the size of the bounding box in each dimension.
    /// </summary>
    public Vector3 Size => Max - Min;

    /// <summary>
    /// Gets the extents (half-size) of the bounding box in each dimension.
    /// </summary>
    public Vector3 Extents => Size * 0.5f;

    /// <summary>
    /// Gets the volume of the bounding box.
    /// </summary>
    public float Volume
    {
        get
        {
            var size = Size;
            return size.X * size.Y * size.Z;
        }
    }

    /// <summary>
    /// Gets the surface area of the bounding box.
    /// </summary>
    public float SurfaceArea
    {
        get
        {
            var size = Size;
            return 2 * (size.X * size.Y + size.Y * size.Z + size.Z * size.X);
        }
    }

    /// <summary>
    /// Gets the diagonal length of the bounding box.
    /// </summary>
    public float DiagonalLength => Size.Length;

    /// <summary>
    /// Gets a value indicating whether this bounding box is empty (has zero volume).
    /// </summary>
    public bool IsEmpty => Min == Max;

    /// <summary>
    /// Determines whether this bounding box contains the specified point.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is contained; otherwise, false.</returns>
    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    /// <summary>
    /// Determines whether this bounding box contains the specified bounding box.
    /// </summary>
    /// <param name="box">The bounding box to test.</param>
    /// <returns>True if the bounding box is contained; otherwise, false.</returns>
    public bool Contains(BoundingBox box)
    {
        return Contains(box.Min) && Contains(box.Max);
    }

    /// <summary>
    /// Determines whether this bounding box intersects with the specified bounding box.
    /// </summary>
    /// <param name="box">The bounding box to test.</param>
    /// <returns>True if the bounding boxes intersect; otherwise, false.</returns>
    public bool Intersects(BoundingBox box)
    {
        return Min.X <= box.Max.X && Max.X >= box.Min.X &&
               Min.Y <= box.Max.Y && Max.Y >= box.Min.Y &&
               Min.Z <= box.Max.Z && Max.Z >= box.Min.Z;
    }

    /// <summary>
    /// Expands this bounding box to include the specified point.
    /// </summary>
    /// <param name="point">The point to include.</param>
    /// <returns>A new expanded bounding box.</returns>
    public BoundingBox Expand(Vector3 point)
    {
        return new BoundingBox(
            Vector3.Min(Min, point),
            Vector3.Max(Max, point)
        );
    }

    /// <summary>
    /// Expands this bounding box to include the specified bounding box.
    /// </summary>
    /// <param name="box">The bounding box to include.</param>
    /// <returns>A new expanded bounding box.</returns>
    public BoundingBox Expand(BoundingBox box)
    {
        return new BoundingBox(
            Vector3.Min(Min, box.Min),
            Vector3.Max(Max, box.Max)
        );
    }

    /// <summary>
    /// Combines this bounding box with another bounding box to create a new bounding box that encompasses both.
    /// </summary>
    /// <param name="other">The other bounding box to combine with.</param>
    /// <returns>A new bounding box that encompasses both input boxes.</returns>
    public BoundingBox Union(BoundingBox other)
    {
        if (IsEmpty)
            return other;

        if (other.IsEmpty)
            return this;

        return new BoundingBox(
            Vector3.Min(Min, other.Min),
            Vector3.Max(Max, other.Max)
        );
    }

    /// <summary>
    /// Expands this bounding box by the specified amount in all directions.
    /// </summary>
    /// <param name="amount">The amount to expand by.</param>
    /// <returns>A new expanded bounding box.</returns>
    public BoundingBox Expand(float amount)
    {
        var expansion = new Vector3(amount);
        return new BoundingBox(Min - expansion, Max + expansion);
    }

    /// <summary>
    /// Gets the closest point on this bounding box to the specified point.
    /// </summary>
    /// <param name="point">The point to find the closest point to.</param>
    /// <returns>The closest point on the bounding box.</returns>
    public Vector3 ClosestPoint(Vector3 point)
    {
        return new Vector3(
            MathF.Max(Min.X, MathF.Min(point.X, Max.X)),
            MathF.Max(Min.Y, MathF.Min(point.Y, Max.Y)),
            MathF.Max(Min.Z, MathF.Min(point.Z, Max.Z))
        );
    }

    /// <summary>
    /// Calculates the distance from this bounding box to the specified point.
    /// </summary>
    /// <param name="point">The point to calculate distance to.</param>
    /// <returns>The distance to the point (0 if the point is inside).</returns>
    public float DistanceTo(Vector3 point)
    {
        var closestPoint = ClosestPoint(point);
        return Vector3.Distance(point, closestPoint);
    }

    /// <summary>
    /// Gets the eight corner points of this bounding box.
    /// </summary>
    /// <returns>An array of the eight corner points.</returns>
    public Vector3[] GetCorners()
    {
        return new[]
        {
            new Vector3(Min.X, Min.Y, Min.Z),
            new Vector3(Max.X, Min.Y, Min.Z),
            new Vector3(Max.X, Max.Y, Min.Z),
            new Vector3(Min.X, Max.Y, Min.Z),
            new Vector3(Min.X, Min.Y, Max.Z),
            new Vector3(Max.X, Min.Y, Max.Z),
            new Vector3(Max.X, Max.Y, Max.Z),
            new Vector3(Min.X, Max.Y, Max.Z)
        };
    }

    /// <summary>
    /// Transforms this bounding box by the specified matrix.
    /// </summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>A new transformed bounding box.</returns>
    public BoundingBox Transform(Matrix4x4 matrix)
    {
        var corners = GetCorners();
        var transformedCorners = corners.Select(corner => matrix.TransformPoint(corner));
        return FromPoints(transformedCorners);
    }

    public bool Equals(BoundingBox other)
    {
        return Min.Equals(other.Min) && Max.Equals(other.Max);
    }

    public override bool Equals(object? obj)
    {
        return obj is BoundingBox other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    public static bool operator ==(BoundingBox left, BoundingBox right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BoundingBox left, BoundingBox right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"[Min: {Min}, Max: {Max}, Size: {Size}]";
    }
}
