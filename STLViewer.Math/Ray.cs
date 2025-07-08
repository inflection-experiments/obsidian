namespace STLViewer.Math;

/// <summary>
/// Represents a ray in 3D space for ray casting and geometric calculations.
/// </summary>
public readonly struct Ray : IEquatable<Ray>
{
    /// <summary>
    /// The origin point of the ray.
    /// </summary>
    public readonly Vector3 Origin;

    /// <summary>
    /// The direction vector of the ray (should be normalized).
    /// </summary>
    public readonly Vector3 Direction;

    /// <summary>
    /// Initializes a new ray.
    /// </summary>
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
    }

    /// <summary>
    /// Gets a point along the ray at the given parameter t.
    /// </summary>
    public Vector3 GetPoint(float t) => Origin + Direction * t;

    /// <summary>
    /// Determines if the ray intersects with a bounding box.
    /// </summary>
    public bool Intersects(BoundingBox box)
    {
        return Intersects(box, out _);
    }

    /// <summary>
    /// Determines if the ray intersects with a bounding box and returns the distance.
    /// </summary>
    public bool Intersects(BoundingBox box, out float distance)
    {
        distance = 0f;

        var invDir = new Vector3(
            MathF.Abs(Direction.X) > float.Epsilon ? 1f / Direction.X : float.MaxValue,
            MathF.Abs(Direction.Y) > float.Epsilon ? 1f / Direction.Y : float.MaxValue,
            MathF.Abs(Direction.Z) > float.Epsilon ? 1f / Direction.Z : float.MaxValue);

        var t1 = (box.Min.X - Origin.X) * invDir.X;
        var t2 = (box.Max.X - Origin.X) * invDir.X;
        var t3 = (box.Min.Y - Origin.Y) * invDir.Y;
        var t4 = (box.Max.Y - Origin.Y) * invDir.Y;
        var t5 = (box.Min.Z - Origin.Z) * invDir.Z;
        var t6 = (box.Max.Z - Origin.Z) * invDir.Z;

        var tmin = System.Math.Max(System.Math.Max(System.Math.Min(t1, t2), System.Math.Min(t3, t4)), System.Math.Min(t5, t6));
        var tmax = System.Math.Min(System.Math.Min(System.Math.Max(t1, t2), System.Math.Max(t3, t4)), System.Math.Max(t5, t6));

        if (tmax < 0 || tmin > tmax)
            return false;

        distance = tmin < 0 ? tmax : tmin;
        return true;
    }

    /// <summary>
    /// Determines if the ray intersects with a triangle defined by three vertices.
    /// </summary>
    public bool Intersects(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return Intersects(v1, v2, v3, out _);
    }

    /// <summary>
    /// Determines if the ray intersects with a triangle defined by three vertices and returns the distance.
    /// Uses the Möller-Trumbore intersection algorithm.
    /// </summary>
    public bool Intersects(Vector3 v1, Vector3 v2, Vector3 v3, out float distance)
    {
        distance = 0f;

        const float epsilon = 0.0000001f;

        var edge1 = v2 - v1;
        var edge2 = v3 - v1;

        var h = Vector3.Cross(Direction, edge2);
        var a = Vector3.Dot(edge1, h);

        if (MathF.Abs(a) < epsilon)
            return false; // Ray is parallel to triangle

        var f = 1f / a;
        var s = Origin - v1;
        var u = f * Vector3.Dot(s, h);

        if (u < 0f || u > 1f)
            return false;

        var q = Vector3.Cross(s, edge1);
        var v = f * Vector3.Dot(Direction, q);

        if (v < 0f || u + v > 1f)
            return false;

        var t = f * Vector3.Dot(edge2, q);

        if (t > epsilon)
        {
            distance = t;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if the ray intersects with a sphere.
    /// </summary>
    public bool Intersects(Vector3 center, float radius)
    {
        return Intersects(center, radius, out _);
    }

    /// <summary>
    /// Determines if the ray intersects with a sphere and returns the distance.
    /// </summary>
    public bool Intersects(Vector3 center, float radius, out float distance)
    {
        distance = 0f;

        var oc = Origin - center;
        var a = Vector3.Dot(Direction, Direction);
        var b = 2f * Vector3.Dot(oc, Direction);
        var c = Vector3.Dot(oc, oc) - radius * radius;

        var discriminant = b * b - 4f * a * c;

        if (discriminant < 0)
            return false;

        var sqrt = MathF.Sqrt(discriminant);
        var t1 = (-b - sqrt) / (2f * a);
        var t2 = (-b + sqrt) / (2f * a);

        if (t1 > 0)
        {
            distance = t1;
            return true;
        }

        if (t2 > 0)
        {
            distance = t2;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if the ray intersects with a plane.
    /// </summary>
    public bool Intersects(Plane plane)
    {
        return Intersects(plane, out _);
    }

    /// <summary>
    /// Determines if the ray intersects with a plane and returns the distance.
    /// </summary>
    public bool Intersects(Plane plane, out float distance)
    {
        distance = 0f;

        var denom = Vector3.Dot(Direction, plane.Normal);

        if (MathF.Abs(denom) < float.Epsilon)
            return false; // Ray is parallel to plane

        var t = (plane.Distance - Vector3.Dot(Origin, plane.Normal)) / denom;

        if (t >= 0)
        {
            distance = t;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Finds the closest point on the ray to a given point.
    /// </summary>
    public Vector3 ClosestPoint(Vector3 point)
    {
        var t = Vector3.Dot(point - Origin, Direction);
        return Origin + Direction * System.Math.Max(0f, t);
    }

    /// <summary>
    /// Calculates the distance from the ray to a point.
    /// </summary>
    public float DistanceToPoint(Vector3 point)
    {
        var closestPoint = ClosestPoint(point);
        return Vector3.Distance(point, closestPoint);
    }

    /// <summary>
    /// Transforms the ray by a matrix.
    /// </summary>
    public Ray Transform(Matrix4x4 matrix)
    {
        var transformedOrigin = matrix.TransformPoint(Origin);
        var transformedDirection = matrix.TransformDirection(Direction);
        return new Ray(transformedOrigin, transformedDirection);
    }

    public bool Equals(Ray other)
    {
        return Origin.Equals(other.Origin) && Direction.Equals(other.Direction);
    }

    public override bool Equals(object? obj)
    {
        return obj is Ray other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Origin, Direction);
    }

    public static bool operator ==(Ray left, Ray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Ray left, Ray right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"Ray(Origin: {Origin}, Direction: {Direction})";
    }
}
