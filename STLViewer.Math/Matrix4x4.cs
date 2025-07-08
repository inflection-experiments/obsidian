using System.Runtime.CompilerServices;

namespace STLViewer.Math;

/// <summary>
/// Represents a 4x4 matrix for 3D transformations.
/// </summary>
public readonly struct Matrix4x4 : IEquatable<Matrix4x4>
{
    /// <summary>
    /// Matrix elements in row-major order.
    /// </summary>
    public readonly float M11, M12, M13, M14;
    public readonly float M21, M22, M23, M24;
    public readonly float M31, M32, M33, M34;
    public readonly float M41, M42, M43, M44;

    /// <summary>
    /// Gets the identity matrix.
    /// </summary>
    public static Matrix4x4 Identity => new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

    /// <summary>
    /// Gets the zero matrix.
    /// </summary>
    public static Matrix4x4 Zero => new(
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0,
        0, 0, 0, 0);

    /// <summary>
    /// Initializes a new Matrix4x4 with the specified values.
    /// </summary>
    public Matrix4x4(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    /// <summary>
    /// Gets the translation component of the matrix.
    /// </summary>
    public Vector3 Translation => new(M41, M42, M43);

    /// <summary>
    /// Gets the determinant of the matrix.
    /// </summary>
    public float Determinant
    {
        get
        {
            var a = M11 * (M22 * (M33 * M44 - M34 * M43) - M23 * (M32 * M44 - M34 * M42) + M24 * (M32 * M43 - M33 * M42));
            var b = M12 * (M21 * (M33 * M44 - M34 * M43) - M23 * (M31 * M44 - M34 * M41) + M24 * (M31 * M43 - M33 * M41));
            var c = M13 * (M21 * (M32 * M44 - M34 * M42) - M22 * (M31 * M44 - M34 * M41) + M24 * (M31 * M42 - M32 * M41));
            var d = M14 * (M21 * (M32 * M43 - M33 * M42) - M22 * (M31 * M43 - M33 * M41) + M23 * (M31 * M42 - M32 * M41));
            return a - b + c - d;
        }
    }

    /// <summary>
    /// Creates a translation matrix.
    /// </summary>
    public static Matrix4x4 CreateTranslation(Vector3 translation)
    {
        return new Matrix4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            translation.X, translation.Y, translation.Z, 1);
    }

    /// <summary>
    /// Creates a scaling matrix.
    /// </summary>
    public static Matrix4x4 CreateScale(Vector3 scale)
    {
        return new Matrix4x4(
            scale.X, 0, 0, 0,
            0, scale.Y, 0, 0,
            0, 0, scale.Z, 0,
            0, 0, 0, 1);
    }

    /// <summary>
    /// Creates a uniform scaling matrix.
    /// </summary>
    public static Matrix4x4 CreateScale(float scale)
    {
        return CreateScale(new Vector3(scale));
    }

    /// <summary>
    /// Creates a rotation matrix around the X axis.
    /// </summary>
    public static Matrix4x4 CreateRotationX(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        return new Matrix4x4(
            1, 0, 0, 0,
            0, cos, sin, 0,
            0, -sin, cos, 0,
            0, 0, 0, 1);
    }

    /// <summary>
    /// Creates a rotation matrix around the Y axis.
    /// </summary>
    public static Matrix4x4 CreateRotationY(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        return new Matrix4x4(
            cos, 0, -sin, 0,
            0, 1, 0, 0,
            sin, 0, cos, 0,
            0, 0, 0, 1);
    }

    /// <summary>
    /// Creates a rotation matrix around the Z axis.
    /// </summary>
    public static Matrix4x4 CreateRotationZ(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);

        return new Matrix4x4(
            cos, sin, 0, 0,
            -sin, cos, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
    }

    /// <summary>
    /// Creates a perspective projection matrix.
    /// </summary>
    public static Matrix4x4 CreatePerspective(float width, float height, float nearPlane, float farPlane)
    {
        if (nearPlane <= 0) throw new ArgumentException("Near plane must be positive", nameof(nearPlane));
        if (farPlane <= 0) throw new ArgumentException("Far plane must be positive", nameof(farPlane));
        if (nearPlane >= farPlane) throw new ArgumentException("Near plane must be less than far plane");

        var twoNear = 2.0f * nearPlane;
        var range = farPlane - nearPlane;

        return new Matrix4x4(
            twoNear / width, 0, 0, 0,
            0, twoNear / height, 0, 0,
            0, 0, -(farPlane + nearPlane) / range, -1,
            0, 0, -(twoNear * farPlane) / range, 0);
    }

    /// <summary>
    /// Creates a perspective projection matrix from field of view.
    /// </summary>
    public static Matrix4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        var height = 2.0f * nearPlane * MathF.Tan(fieldOfView * 0.5f);
        var width = height * aspectRatio;
        return CreatePerspective(width, height, nearPlane, farPlane);
    }

    /// <summary>
    /// Creates an orthographic projection matrix.
    /// </summary>
    public static Matrix4x4 CreateOrthographic(float width, float height, float nearPlane, float farPlane)
    {
        var range = farPlane - nearPlane;

        return new Matrix4x4(
            2.0f / width, 0, 0, 0,
            0, 2.0f / height, 0, 0,
            0, 0, -2.0f / range, 0,
            0, 0, -(farPlane + nearPlane) / range, 1);
    }

    /// <summary>
    /// Creates a look-at view matrix.
    /// </summary>
    public static Matrix4x4 CreateLookAt(Vector3 eye, Vector3 target, Vector3 up)
    {
        var forward = (target - eye).Normalized();
        var right = Vector3.Cross(forward, up).Normalized();
        var actualUp = Vector3.Cross(right, forward);

        return new Matrix4x4(
            right.X, actualUp.X, -forward.X, 0,
            right.Y, actualUp.Y, -forward.Y, 0,
            right.Z, actualUp.Z, -forward.Z, 0,
            -Vector3.Dot(right, eye), -Vector3.Dot(actualUp, eye), Vector3.Dot(forward, eye), 1);
    }

    /// <summary>
    /// Creates a world transformation matrix.
    /// </summary>
    public static Matrix4x4 CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
    {
        var normalizedForward = forward.Normalized();
        var right = Vector3.Cross(up, normalizedForward).Normalized();
        var actualUp = Vector3.Cross(normalizedForward, right);

        return new Matrix4x4(
            right.X, right.Y, right.Z, 0,
            actualUp.X, actualUp.Y, actualUp.Z, 0,
            normalizedForward.X, normalizedForward.Y, normalizedForward.Z, 0,
            position.X, position.Y, position.Z, 1);
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Multiply(Matrix4x4 left, Matrix4x4 right)
    {
        return new Matrix4x4(
            left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41,
            left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42,
            left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43,
            left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44,

            left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41,
            left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42,
            left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43,
            left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44,

            left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41,
            left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42,
            left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43,
            left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44,

            left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41,
            left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42,
            left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43,
            left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44);
    }

    /// <summary>
    /// Calculates the inverse of the matrix.
    /// </summary>
    public Matrix4x4 Inverse()
    {
        var det = Determinant;
        if (MathF.Abs(det) < float.Epsilon)
            throw new InvalidOperationException("Matrix is not invertible");

        var invDet = 1.0f / det;

        return new Matrix4x4(
            invDet * (M22 * (M33 * M44 - M34 * M43) - M23 * (M32 * M44 - M34 * M42) + M24 * (M32 * M43 - M33 * M42)),
            -invDet * (M12 * (M33 * M44 - M34 * M43) - M13 * (M32 * M44 - M34 * M42) + M14 * (M32 * M43 - M33 * M42)),
            invDet * (M12 * (M23 * M44 - M24 * M43) - M13 * (M22 * M44 - M24 * M42) + M14 * (M22 * M43 - M23 * M42)),
            -invDet * (M12 * (M23 * M34 - M24 * M33) - M13 * (M22 * M34 - M24 * M32) + M14 * (M22 * M33 - M23 * M32)),

            -invDet * (M21 * (M33 * M44 - M34 * M43) - M23 * (M31 * M44 - M34 * M41) + M24 * (M31 * M43 - M33 * M41)),
            invDet * (M11 * (M33 * M44 - M34 * M43) - M13 * (M31 * M44 - M34 * M41) + M14 * (M31 * M43 - M33 * M41)),
            -invDet * (M11 * (M23 * M44 - M24 * M43) - M13 * (M21 * M44 - M24 * M41) + M14 * (M21 * M43 - M23 * M41)),
            invDet * (M11 * (M23 * M34 - M24 * M33) - M13 * (M21 * M34 - M24 * M31) + M14 * (M21 * M33 - M23 * M31)),

            invDet * (M21 * (M32 * M44 - M34 * M42) - M22 * (M31 * M44 - M34 * M41) + M24 * (M31 * M42 - M32 * M41)),
            -invDet * (M11 * (M32 * M44 - M34 * M42) - M12 * (M31 * M44 - M34 * M41) + M14 * (M31 * M42 - M32 * M41)),
            invDet * (M11 * (M22 * M44 - M24 * M42) - M12 * (M21 * M44 - M24 * M41) + M14 * (M21 * M42 - M22 * M41)),
            -invDet * (M11 * (M22 * M34 - M24 * M32) - M12 * (M21 * M34 - M24 * M31) + M14 * (M21 * M32 - M22 * M31)),

            -invDet * (M21 * (M32 * M43 - M33 * M42) - M22 * (M31 * M43 - M33 * M41) + M23 * (M31 * M42 - M32 * M41)),
            invDet * (M11 * (M32 * M43 - M33 * M42) - M12 * (M31 * M43 - M33 * M41) + M13 * (M31 * M42 - M32 * M41)),
            -invDet * (M11 * (M22 * M43 - M23 * M42) - M12 * (M21 * M43 - M23 * M41) + M13 * (M21 * M42 - M22 * M41)),
            invDet * (M11 * (M22 * M33 - M23 * M32) - M12 * (M21 * M33 - M23 * M31) + M13 * (M21 * M32 - M22 * M31)));
    }

    /// <summary>
    /// Transposes the matrix.
    /// </summary>
    public Matrix4x4 Transpose()
    {
        return new Matrix4x4(
            M11, M21, M31, M41,
            M12, M22, M32, M42,
            M13, M23, M33, M43,
            M14, M24, M34, M44);
    }

    /// <summary>
    /// Transforms a point (considering translation).
    /// </summary>
    public Vector3 TransformPoint(Vector3 point)
    {
        return new Vector3(
            point.X * M11 + point.Y * M21 + point.Z * M31 + M41,
            point.X * M12 + point.Y * M22 + point.Z * M32 + M42,
            point.X * M13 + point.Y * M23 + point.Z * M33 + M43);
    }

    /// <summary>
    /// Transforms a direction vector (ignoring translation).
    /// </summary>
    public Vector3 TransformDirection(Vector3 direction)
    {
        return new Vector3(
            direction.X * M11 + direction.Y * M21 + direction.Z * M31,
            direction.X * M12 + direction.Y * M22 + direction.Z * M32,
            direction.X * M13 + direction.Y * M23 + direction.Z * M33);
    }

    /// <summary>
    /// Converts to System.Numerics.Matrix4x4.
    /// </summary>
    public System.Numerics.Matrix4x4 ToSystemMatrix4x4()
    {
        return new System.Numerics.Matrix4x4(
            M11, M12, M13, M14,
            M21, M22, M23, M24,
            M31, M32, M33, M34,
            M41, M42, M43, M44);
    }

    /// <summary>
    /// Creates from System.Numerics.Matrix4x4.
    /// </summary>
    public static Matrix4x4 FromSystemMatrix4x4(System.Numerics.Matrix4x4 matrix)
    {
        return new Matrix4x4(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44);
    }

    // Operators
    public static Matrix4x4 operator +(Matrix4x4 left, Matrix4x4 right)
    {
        return new Matrix4x4(
            left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13, left.M14 + right.M14,
            left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23, left.M24 + right.M24,
            left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33, left.M34 + right.M34,
            left.M41 + right.M41, left.M42 + right.M42, left.M43 + right.M43, left.M44 + right.M44);
    }

    public static Matrix4x4 operator -(Matrix4x4 left, Matrix4x4 right)
    {
        return new Matrix4x4(
            left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13, left.M14 - right.M14,
            left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23, left.M24 - right.M24,
            left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33, left.M34 - right.M34,
            left.M41 - right.M41, left.M42 - right.M42, left.M43 - right.M43, left.M44 - right.M44);
    }

    public static Matrix4x4 operator *(Matrix4x4 left, Matrix4x4 right)
    {
        return Multiply(left, right);
    }

    public static Matrix4x4 operator *(Matrix4x4 matrix, float scalar)
    {
        return new Matrix4x4(
            matrix.M11 * scalar, matrix.M12 * scalar, matrix.M13 * scalar, matrix.M14 * scalar,
            matrix.M21 * scalar, matrix.M22 * scalar, matrix.M23 * scalar, matrix.M24 * scalar,
            matrix.M31 * scalar, matrix.M32 * scalar, matrix.M33 * scalar, matrix.M34 * scalar,
            matrix.M41 * scalar, matrix.M42 * scalar, matrix.M43 * scalar, matrix.M44 * scalar);
    }

    public static Matrix4x4 operator *(float scalar, Matrix4x4 matrix)
    {
        return matrix * scalar;
    }

    public static bool operator ==(Matrix4x4 left, Matrix4x4 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Matrix4x4 left, Matrix4x4 right)
    {
        return !left.Equals(right);
    }

    public bool Equals(Matrix4x4 other)
    {
        return M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) && M14.Equals(other.M14) &&
               M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) && M24.Equals(other.M24) &&
               M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33) && M34.Equals(other.M34) &&
               M41.Equals(other.M41) && M42.Equals(other.M42) && M43.Equals(other.M43) && M44.Equals(other.M44);
    }

    public override bool Equals(object? obj)
    {
        return obj is Matrix4x4 other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(M11); hash.Add(M12); hash.Add(M13); hash.Add(M14);
        hash.Add(M21); hash.Add(M22); hash.Add(M23); hash.Add(M24);
        hash.Add(M31); hash.Add(M32); hash.Add(M33); hash.Add(M34);
        hash.Add(M41); hash.Add(M42); hash.Add(M43); hash.Add(M44);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return $"Matrix4x4(\n" +
               $"  [{M11:F3}, {M12:F3}, {M13:F3}, {M14:F3}]\n" +
               $"  [{M21:F3}, {M22:F3}, {M23:F3}, {M24:F3}]\n" +
               $"  [{M31:F3}, {M32:F3}, {M33:F3}, {M34:F3}]\n" +
               $"  [{M41:F3}, {M42:F3}, {M43:F3}, {M44:F3}]\n" +
               $")";
    }
}
