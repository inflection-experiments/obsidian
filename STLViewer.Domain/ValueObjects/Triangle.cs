using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents a triangle in 3D space with three vertices and a normal vector.
/// </summary>
public readonly record struct Triangle
{
    /// <summary>
    /// The first vertex of the triangle.
    /// </summary>
    public Vector3 Vertex1 { get; }

    /// <summary>
    /// The second vertex of the triangle.
    /// </summary>
    public Vector3 Vertex2 { get; }

    /// <summary>
    /// The third vertex of the triangle.
    /// </summary>
    public Vector3 Vertex3 { get; }

    /// <summary>
    /// The normal vector of the triangle.
    /// </summary>
    public Vector3 Normal { get; }

    /// <summary>
    /// Initializes a new instance of the Triangle struct.
    /// </summary>
    /// <param name="vertex1">The first vertex.</param>
    /// <param name="vertex2">The second vertex.</param>
    /// <param name="vertex3">The third vertex.</param>
    /// <param name="normal">The normal vector.</param>
    public Triangle(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 normal)
    {
        Vertex1 = vertex1;
        Vertex2 = vertex2;
        Vertex3 = vertex3;
        Normal = normal;
    }

    /// <summary>
    /// Creates a triangle with automatically calculated normal.
    /// </summary>
    /// <param name="vertex1">The first vertex.</param>
    /// <param name="vertex2">The second vertex.</param>
    /// <param name="vertex3">The third vertex.</param>
    /// <returns>A new triangle with calculated normal.</returns>
    public static Triangle Create(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        var normal = CalculateNormal(vertex1, vertex2, vertex3);
        return new Triangle(vertex1, vertex2, vertex3, normal);
    }

    /// <summary>
    /// Calculates the normal vector for three vertices.
    /// </summary>
    /// <param name="vertex1">The first vertex.</param>
    /// <param name="vertex2">The second vertex.</param>
    /// <param name="vertex3">The third vertex.</param>
    /// <returns>The calculated normal vector.</returns>
    public static Vector3 CalculateNormal(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3)
    {
        var edge1 = vertex2 - vertex1;
        var edge2 = vertex3 - vertex1;
        return Vector3.Cross(edge1, edge2).Normalized();
    }

    /// <summary>
    /// Gets the area of the triangle.
    /// </summary>
    public float Area
    {
        get
        {
            var edge1 = Vertex2 - Vertex1;
            var edge2 = Vertex3 - Vertex1;
            return 0.5f * Vector3.Cross(edge1, edge2).Length;
        }
    }

    /// <summary>
    /// Gets the perimeter of the triangle.
    /// </summary>
    public float Perimeter
    {
        get
        {
            var side1 = Vector3.Distance(Vertex1, Vertex2);
            var side2 = Vector3.Distance(Vertex2, Vertex3);
            var side3 = Vector3.Distance(Vertex3, Vertex1);
            return side1 + side2 + side3;
        }
    }

    /// <summary>
    /// Gets the centroid (center point) of the triangle.
    /// </summary>
    public Vector3 Centroid => (Vertex1 + Vertex2 + Vertex3) / 3.0f;

    /// <summary>
    /// Gets the bounding box that encompasses this triangle.
    /// </summary>
    public BoundingBox BoundingBox
    {
        get
        {
            var min = Vector3.Min(Vector3.Min(Vertex1, Vertex2), Vertex3);
            var max = Vector3.Max(Vector3.Max(Vertex1, Vertex2), Vertex3);
            return new BoundingBox(min, max);
        }
    }

    /// <summary>
    /// Gets the three vertices as an array.
    /// </summary>
    public Vector3[] Vertices => new[] { Vertex1, Vertex2, Vertex3 };

    /// <summary>
    /// Gets the three edge vectors of the triangle.
    /// </summary>
    public Vector3[] Edges => new[]
    {
        Vertex2 - Vertex1,
        Vertex3 - Vertex2,
        Vertex1 - Vertex3
    };

    /// <summary>
    /// Determines whether a point is inside this triangle using barycentric coordinates.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is inside the triangle; otherwise, false.</returns>
    public bool Contains(Vector3 point)
    {
        // Project the triangle and point onto the plane perpendicular to the normal
        var v0 = Vertex3 - Vertex1;
        var v1 = Vertex2 - Vertex1;
        var v2 = point - Vertex1;

        // Compute dot products
        var dot00 = Vector3.Dot(v0, v0);
        var dot01 = Vector3.Dot(v0, v1);
        var dot02 = Vector3.Dot(v0, v2);
        var dot11 = Vector3.Dot(v1, v1);
        var dot12 = Vector3.Dot(v1, v2);

        // Compute barycentric coordinates
        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // Check if point is in triangle
        return (u >= 0) && (v >= 0) && (u + v <= 1);
    }

    /// <summary>
    /// Calculates the closest point on the triangle to the specified point.
    /// </summary>
    /// <param name="point">The point to find the closest point to.</param>
    /// <returns>The closest point on the triangle.</returns>
    public Vector3 ClosestPoint(Vector3 point)
    {
        // Project point onto triangle plane
        var triangleToPoint = point - Vertex1;
        var projectedDistance = Vector3.Dot(triangleToPoint, Normal);
        var projectedPoint = point - Normal * projectedDistance;

        // Check if projected point is inside triangle
        if (Contains(projectedPoint))
            return projectedPoint;

        // Find closest point on triangle edges
        var closestOnEdge1 = ClosestPointOnLineSegment(projectedPoint, Vertex1, Vertex2);
        var closestOnEdge2 = ClosestPointOnLineSegment(projectedPoint, Vertex2, Vertex3);
        var closestOnEdge3 = ClosestPointOnLineSegment(projectedPoint, Vertex3, Vertex1);

        var dist1 = Vector3.DistanceSquared(projectedPoint, closestOnEdge1);
        var dist2 = Vector3.DistanceSquared(projectedPoint, closestOnEdge2);
        var dist3 = Vector3.DistanceSquared(projectedPoint, closestOnEdge3);

        if (dist1 <= dist2 && dist1 <= dist3)
            return closestOnEdge1;

        return dist2 <= dist3 ? closestOnEdge2 : closestOnEdge3;
    }

    /// <summary>
    /// Transforms this triangle by the specified matrix.
    /// </summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>A new transformed triangle.</returns>
    public Triangle Transform(Matrix4x4 matrix)
    {
        var transformedVertex1 = matrix.TransformPoint(Vertex1);
        var transformedVertex2 = matrix.TransformPoint(Vertex2);
        var transformedVertex3 = matrix.TransformPoint(Vertex3);
        var transformedNormal = matrix.TransformDirection(Normal).Normalized();

        return new Triangle(transformedVertex1, transformedVertex2, transformedVertex3, transformedNormal);
    }

    /// <summary>
    /// Determines whether this triangle is degenerate (has zero area).
    /// </summary>
    public bool IsDegenerate => Area < float.Epsilon;

    /// <summary>
    /// Determines whether this triangle is valid (has finite vertices and a valid normal).
    /// </summary>
    public bool IsValid
    {
        get
        {
            return IsFinite(Vertex1) && IsFinite(Vertex2) && IsFinite(Vertex3) &&
                   IsFinite(Normal) && !IsDegenerate;
        }
    }

    private static bool IsFinite(Vector3 vector)
    {
        return float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);
    }

    private static Vector3 ClosestPointOnLineSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        var segment = end - start;
        var pointToStart = point - start;
        var t = Vector3.Dot(pointToStart, segment) / Vector3.Dot(segment, segment);
        t = MathF.Max(0, MathF.Min(1, t));
        return start + segment * t;
    }

    public override string ToString()
    {
        return $"Triangle[V1: {Vertex1}, V2: {Vertex2}, V3: {Vertex3}, Normal: {Normal}, Area: {Area:F3}]";
    }
}
