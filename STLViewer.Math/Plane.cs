namespace STLViewer.Math;

/// <summary>
/// Represents a plane in 3D space defined by a normal vector and distance from origin.
/// </summary>
public readonly struct Plane : IEquatable<Plane>
{
    /// <summary>
    /// The normal vector of the plane.
    /// </summary>
    public readonly Vector3 Normal;

    /// <summary>
    /// The distance from the origin to the plane along the normal.
    /// </summary>
    public readonly float Distance;

    /// <summary>
    /// Initializes a new plane from a normal vector and distance.
    /// </summary>
    public Plane(Vector3 normal, float distance)
    {
        Normal = normal.Normalized();
        Distance = distance;
    }

    /// <summary>
    /// Initializes a new plane from a normal vector and a point on the plane.
    /// </summary>
    public Plane(Vector3 normal, Vector3 point)
    {
        Normal = normal.Normalized();
        Distance = Vector3.Dot(Normal, point);
    }

    /// <summary>
    /// Initializes a new plane from three points.
    /// </summary>
    public Plane(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        var v1 = point2 - point1;
        var v2 = point3 - point1;
        Normal = Vector3.Cross(v1, v2).Normalized();
        Distance = Vector3.Dot(Normal, point1);
    }

    /// <summary>
    /// Calculates the signed distance from the plane to a point.
    /// Positive if on the side of the normal, negative otherwise.
    /// </summary>
    public float DistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(Normal, point) - Distance;
    }

    /// <summary>
    /// Determines which side of the plane a point is on.
    /// </summary>
    public PlaneSide GetSide(Vector3 point)
    {
        var distance = DistanceToPoint(point);
        return distance > float.Epsilon ? PlaneSide.Positive :
               distance < -float.Epsilon ? PlaneSide.Negative :
               PlaneSide.OnPlane;
    }

    /// <summary>
    /// Projects a point onto the plane.
    /// </summary>
    public Vector3 ClosestPoint(Vector3 point)
    {
        var distance = DistanceToPoint(point);
        return point - Normal * distance;
    }

    /// <summary>
    /// Reflects a point across the plane.
    /// </summary>
    public Vector3 Reflect(Vector3 point)
    {
        var distance = DistanceToPoint(point);
        return point - Normal * (2f * distance);
    }

    /// <summary>
    /// Reflects a vector across the plane.
    /// </summary>
    public Vector3 ReflectVector(Vector3 vector)
    {
        return vector - 2f * Vector3.Dot(vector, Normal) * Normal;
    }

    /// <summary>
    /// Determines if the plane intersects with a bounding box.
    /// </summary>
    public bool Intersects(BoundingBox box)
    {
        var center = box.Center;
        var extents = box.Extents;

        var radius = MathF.Abs(extents.X * Normal.X) +
                     MathF.Abs(extents.Y * Normal.Y) +
                     MathF.Abs(extents.Z * Normal.Z);

        var distance = MathF.Abs(DistanceToPoint(center));
        return distance <= radius;
    }

    /// <summary>
    /// Determines if the plane intersects with a sphere.
    /// </summary>
    public bool Intersects(Vector3 center, float radius)
    {
        var distance = MathF.Abs(DistanceToPoint(center));
        return distance <= radius;
    }

    /// <summary>
    /// Transforms the plane by a matrix.
    /// </summary>
    public Plane Transform(Matrix4x4 matrix)
    {
        var inverseTranspose = matrix.Inverse().Transpose();
        var transformedNormal = inverseTranspose.TransformDirection(Normal);
        var pointOnPlane = Normal * Distance;
        var transformedPoint = matrix.TransformPoint(pointOnPlane);
        return new Plane(transformedNormal, transformedPoint);
    }

    /// <summary>
    /// Flips the plane (negates normal and distance).
    /// </summary>
    public Plane Flip()
    {
        return new Plane(-Normal, -Distance);
    }

    public bool Equals(Plane other)
    {
        return Normal.Equals(other.Normal) && Distance.Equals(other.Distance);
    }

    public override bool Equals(object? obj)
    {
        return obj is Plane other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Normal, Distance);
    }

    public static bool operator ==(Plane left, Plane right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Plane left, Plane right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"Plane(Normal: {Normal}, Distance: {Distance:F3})";
    }
}

/// <summary>
/// Represents which side of a plane a point is on.
/// </summary>
public enum PlaneSide
{
    /// <summary>
    /// The point is on the plane.
    /// </summary>
    OnPlane,

    /// <summary>
    /// The point is on the positive side of the plane (in the direction of the normal).
    /// </summary>
    Positive,

    /// <summary>
    /// The point is on the negative side of the plane (opposite to the normal).
    /// </summary>
    Negative
}
