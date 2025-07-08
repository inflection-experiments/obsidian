using System.Numerics;
using System.Runtime.CompilerServices;

namespace STLViewer.Math;

/// <summary>
/// Represents a 3D vector with single-precision floating-point components.
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
{
    /// <summary>
    /// The X component of the vector.
    /// </summary>
    public readonly float X;

    /// <summary>
    /// The Y component of the vector.
    /// </summary>
    public readonly float Y;

    /// <summary>
    /// The Z component of the vector.
    /// </summary>
    public readonly float Z;

    /// <summary>
    /// Gets a vector with all components set to zero.
    /// </summary>
    public static Vector3 Zero => new(0, 0, 0);

    /// <summary>
    /// Gets a vector with all components set to one.
    /// </summary>
    public static Vector3 One => new(1, 1, 1);

    /// <summary>
    /// Gets the unit vector for the X axis.
    /// </summary>
    public static Vector3 UnitX => new(1, 0, 0);

    /// <summary>
    /// Gets the unit vector for the Y axis.
    /// </summary>
    public static Vector3 UnitY => new(0, 1, 0);

    /// <summary>
    /// Gets the unit vector for the Z axis.
    /// </summary>
    public static Vector3 UnitZ => new(0, 0, 1);

    /// <summary>
    /// Initializes a new instance of the Vector3 struct.
    /// </summary>
    /// <param name="x">The X component.</param>
    /// <param name="y">The Y component.</param>
    /// <param name="z">The Z component.</param>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Initializes a new instance of the Vector3 struct with all components set to the same value.
    /// </summary>
    /// <param name="value">The value to set all components to.</param>
    public Vector3(float value)
    {
        X = Y = Z = value;
    }

    /// <summary>
    /// Gets the squared length of the vector.
    /// </summary>
    public float LengthSquared => X * X + Y * Y + Z * Z;

    /// <summary>
    /// Gets the length of the vector.
    /// </summary>
    public float Length => MathF.Sqrt(LengthSquared);

    /// <summary>
    /// Returns a normalized version of this vector.
    /// </summary>
    /// <returns>A normalized vector.</returns>
    public Vector3 Normalized()
    {
        var length = Length;
        return length > 0 ? this / length : Zero;
    }

    /// <summary>
    /// Calculates the dot product of two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The dot product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 left, Vector3 right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    /// <summary>
    /// Calculates the cross product of two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The cross product.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Cross(Vector3 left, Vector3 right)
    {
        return new Vector3(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X
        );
    }

    /// <summary>
    /// Calculates the distance between two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3 left, Vector3 right)
    {
        return (left - right).Length;
    }

    /// <summary>
    /// Calculates the squared distance between two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>The squared distance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector3 left, Vector3 right)
    {
        return (left - right).LengthSquared;
    }

    /// <summary>
    /// Linearly interpolates between two vectors.
    /// </summary>
    /// <param name="start">The start vector.</param>
    /// <param name="end">The end vector.</param>
    /// <param name="t">The interpolation factor.</param>
    /// <returns>The interpolated vector.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Lerp(Vector3 start, Vector3 end, float t)
    {
        return start + (end - start) * t;
    }

    /// <summary>
    /// Returns the minimum components of two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A vector with the minimum components.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Min(Vector3 left, Vector3 right)
    {
        return new Vector3(
            MathF.Min(left.X, right.X),
            MathF.Min(left.Y, right.Y),
            MathF.Min(left.Z, right.Z)
        );
    }

    /// <summary>
    /// Returns the maximum components of two vectors.
    /// </summary>
    /// <param name="left">The first vector.</param>
    /// <param name="right">The second vector.</param>
    /// <returns>A vector with the maximum components.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Max(Vector3 left, Vector3 right)
    {
        return new Vector3(
            MathF.Max(left.X, right.X),
            MathF.Max(left.Y, right.Y),
            MathF.Max(left.Z, right.Z)
        );
    }

    /// <summary>
    /// Transforms a vector by a matrix.
    /// </summary>
    /// <param name="vector">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector3 Transform(Vector3 vector, Matrix4x4 matrix)
    {
        return new Vector3(
            vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31 + matrix.M41,
            vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32 + matrix.M42,
            vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33 + matrix.M43
        );
    }

    /// <summary>
    /// Transforms a vector's direction by a matrix (ignoring translation).
    /// </summary>
    /// <param name="vector">The vector to transform.</param>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>The transformed vector.</returns>
    public static Vector3 TransformDirection(Vector3 vector, Matrix4x4 matrix)
    {
        return new Vector3(
            vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31,
            vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32,
            vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33
        );
    }

    /// <summary>
    /// Converts to System.Numerics.Vector3.
    /// </summary>
    /// <returns>A System.Numerics.Vector3.</returns>
    public System.Numerics.Vector3 ToSystemVector3()
    {
        return new System.Numerics.Vector3(X, Y, Z);
    }

    /// <summary>
    /// Creates a Vector3 from System.Numerics.Vector3.
    /// </summary>
    /// <param name="vector">The System.Numerics.Vector3.</param>
    /// <returns>A Vector3.</returns>
    public static Vector3 FromSystemVector3(System.Numerics.Vector3 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    // Operators
    public static Vector3 operator +(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vector3 operator -(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Vector3 operator *(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    public static Vector3 operator *(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    public static Vector3 operator *(float scalar, Vector3 vector)
    {
        return vector * scalar;
    }

    public static Vector3 operator /(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    public static Vector3 operator /(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X / scalar, vector.Y / scalar, vector.Z / scalar);
    }

    public static Vector3 operator -(Vector3 vector)
    {
        return new Vector3(-vector.X, -vector.Y, -vector.Z);
    }

    public static bool operator ==(Vector3 left, Vector3 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Vector3 left, Vector3 right)
    {
        return !left.Equals(right);
    }

    public bool Equals(Vector3 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }

    public override bool Equals(object? obj)
    {
        return obj is Vector3 other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }

    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}
