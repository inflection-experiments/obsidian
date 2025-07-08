using System.Runtime.CompilerServices;

namespace STLViewer.Math;

/// <summary>
/// Represents a quaternion for 3D rotations.
/// </summary>
public readonly struct Quaternion : IEquatable<Quaternion>
{
    /// <summary>
    /// The X component of the quaternion.
    /// </summary>
    public readonly float X;

    /// <summary>
    /// The Y component of the quaternion.
    /// </summary>
    public readonly float Y;

    /// <summary>
    /// The Z component of the quaternion.
    /// </summary>
    public readonly float Z;

    /// <summary>
    /// The W component of the quaternion.
    /// </summary>
    public readonly float W;

    /// <summary>
    /// Gets the identity quaternion.
    /// </summary>
    public static Quaternion Identity => new(0, 0, 0, 1);

    /// <summary>
    /// Gets the zero quaternion.
    /// </summary>
    public static Quaternion Zero => new(0, 0, 0, 0);

    /// <summary>
    /// Initializes a new quaternion.
    /// </summary>
    public Quaternion(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Initializes a new quaternion from a vector and scalar.
    /// </summary>
    public Quaternion(Vector3 vector, float scalar)
    {
        X = vector.X;
        Y = vector.Y;
        Z = vector.Z;
        W = scalar;
    }

    /// <summary>
    /// Gets the length of the quaternion.
    /// </summary>
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);

    /// <summary>
    /// Gets the squared length of the quaternion.
    /// </summary>
    public float LengthSquared => X * X + Y * Y + Z * Z + W * W;

    /// <summary>
    /// Gets the normalized quaternion.
    /// </summary>
    public Quaternion Normalized
    {
        get
        {
            var length = Length;
            if (length < float.Epsilon)
                return Identity;
            return new Quaternion(X / length, Y / length, Z / length, W / length);
        }
    }

    /// <summary>
    /// Gets the conjugate of the quaternion.
    /// </summary>
    public Quaternion Conjugate => new(-X, -Y, -Z, W);

    /// <summary>
    /// Gets the inverse of the quaternion.
    /// </summary>
    public Quaternion Inverse
    {
        get
        {
            var lengthSq = LengthSquared;
            if (lengthSq < float.Epsilon)
                throw new InvalidOperationException("Cannot invert a zero quaternion");

            var invLengthSq = 1.0f / lengthSq;
            return new Quaternion(-X * invLengthSq, -Y * invLengthSq, -Z * invLengthSq, W * invLengthSq);
        }
    }

    /// <summary>
    /// Creates a quaternion from an axis and angle.
    /// </summary>
    public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
    {
        var halfAngle = angle * 0.5f;
        var sin = MathF.Sin(halfAngle);
        var cos = MathF.Cos(halfAngle);

        var normalizedAxis = axis.Normalized();
        return new Quaternion(
            normalizedAxis.X * sin,
            normalizedAxis.Y * sin,
            normalizedAxis.Z * sin,
            cos);
    }

    /// <summary>
    /// Creates a quaternion from Euler angles (in radians).
    /// </summary>
    public static Quaternion CreateFromEuler(float pitch, float yaw, float roll)
    {
        var halfPitch = pitch * 0.5f;
        var halfYaw = yaw * 0.5f;
        var halfRoll = roll * 0.5f;

        var sinPitch = MathF.Sin(halfPitch);
        var cosPitch = MathF.Cos(halfPitch);
        var sinYaw = MathF.Sin(halfYaw);
        var cosYaw = MathF.Cos(halfYaw);
        var sinRoll = MathF.Sin(halfRoll);
        var cosRoll = MathF.Cos(halfRoll);

        return new Quaternion(
            sinPitch * cosYaw * cosRoll - cosPitch * sinYaw * sinRoll,
            cosPitch * sinYaw * cosRoll + sinPitch * cosYaw * sinRoll,
            cosPitch * cosYaw * sinRoll - sinPitch * sinYaw * cosRoll,
            cosPitch * cosYaw * cosRoll + sinPitch * sinYaw * sinRoll);
    }

    /// <summary>
    /// Creates a quaternion from a rotation matrix.
    /// </summary>
    public static Quaternion CreateFromRotationMatrix(Matrix4x4 matrix)
    {
        var trace = matrix.M11 + matrix.M22 + matrix.M33;

        if (trace > 0.0f)
        {
            var s = MathF.Sqrt(trace + 1.0f) * 2.0f;
            return new Quaternion(
                (matrix.M32 - matrix.M23) / s,
                (matrix.M13 - matrix.M31) / s,
                (matrix.M21 - matrix.M12) / s,
                s * 0.25f);
        }
        else if (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33)
        {
            var s = MathF.Sqrt(1.0f + matrix.M11 - matrix.M22 - matrix.M33) * 2.0f;
            return new Quaternion(
                s * 0.25f,
                (matrix.M12 + matrix.M21) / s,
                (matrix.M13 + matrix.M31) / s,
                (matrix.M32 - matrix.M23) / s);
        }
        else if (matrix.M22 > matrix.M33)
        {
            var s = MathF.Sqrt(1.0f + matrix.M22 - matrix.M11 - matrix.M33) * 2.0f;
            return new Quaternion(
                (matrix.M12 + matrix.M21) / s,
                s * 0.25f,
                (matrix.M23 + matrix.M32) / s,
                (matrix.M13 - matrix.M31) / s);
        }
        else
        {
            var s = MathF.Sqrt(1.0f + matrix.M33 - matrix.M11 - matrix.M22) * 2.0f;
            return new Quaternion(
                (matrix.M13 + matrix.M31) / s,
                (matrix.M23 + matrix.M32) / s,
                s * 0.25f,
                (matrix.M21 - matrix.M12) / s);
        }
    }

    /// <summary>
    /// Converts the quaternion to a rotation matrix.
    /// </summary>
    public Matrix4x4 ToRotationMatrix()
    {
        var xx = X * X;
        var yy = Y * Y;
        var zz = Z * Z;
        var xy = X * Y;
        var xz = X * Z;
        var yz = Y * Z;
        var wx = W * X;
        var wy = W * Y;
        var wz = W * Z;

        return new Matrix4x4(
            1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy), 0,
            2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx), 0,
            2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy), 0,
            0, 0, 0, 1);
    }

    /// <summary>
    /// Gets the Euler angles from the quaternion (in radians).
    /// </summary>
    public Vector3 ToEuler()
    {
        var sinr_cosp = 2 * (W * X + Y * Z);
        var cosr_cosp = 1 - 2 * (X * X + Y * Y);
        var roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        var sinp = 2 * (W * Y - Z * X);
        var pitch = MathF.Abs(sinp) >= 1
            ? MathF.CopySign(MathF.PI / 2, sinp)
            : MathF.Asin(sinp);

        var siny_cosp = 2 * (W * Z + X * Y);
        var cosy_cosp = 1 - 2 * (Y * Y + Z * Z);
        var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        return new Vector3(pitch, yaw, roll);
    }

    /// <summary>
    /// Multiplies two quaternions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Multiply(Quaternion left, Quaternion right)
    {
        return new Quaternion(
            left.W * right.X + left.X * right.W + left.Y * right.Z - left.Z * right.Y,
            left.W * right.Y - left.X * right.Z + left.Y * right.W + left.Z * right.X,
            left.W * right.Z + left.X * right.Y - left.Y * right.X + left.Z * right.W,
            left.W * right.W - left.X * right.X - left.Y * right.Y - left.Z * right.Z);
    }

    /// <summary>
    /// Computes the dot product of two quaternions.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Quaternion left, Quaternion right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
    }

    /// <summary>
    /// Spherical linear interpolation between two quaternions.
    /// </summary>
    public static Quaternion Slerp(Quaternion from, Quaternion to, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);

        var dot = Dot(from, to);

        // If the dot product is negative, slerp won't take the shorter path
        if (dot < 0.0f)
        {
            to = -to;
            dot = -dot;
        }

        const float dotThreshold = 0.9995f;
        if (dot > dotThreshold)
        {
            // Use linear interpolation for very close quaternions
            var result = from + t * (to - from);
            return result.Normalized;
        }

        var theta = MathF.Acos(dot);
        var sinTheta = MathF.Sin(theta);
        var invSinTheta = 1.0f / sinTheta;

        var weightFrom = MathF.Sin((1.0f - t) * theta) * invSinTheta;
        var weightTo = MathF.Sin(t * theta) * invSinTheta;

        return new Quaternion(
            weightFrom * from.X + weightTo * to.X,
            weightFrom * from.Y + weightTo * to.Y,
            weightFrom * from.Z + weightTo * to.Z,
            weightFrom * from.W + weightTo * to.W);
    }

    /// <summary>
    /// Normalized linear interpolation between two quaternions.
    /// </summary>
    public static Quaternion Nlerp(Quaternion from, Quaternion to, float t)
    {
        t = System.Math.Clamp(t, 0f, 1f);

        var dot = Dot(from, to);

        // If the dot product is negative, nlerp won't take the shorter path
        if (dot < 0.0f)
        {
            to = -to;
        }

        var result = from + t * (to - from);
        return result.Normalized;
    }

    /// <summary>
    /// Rotates a vector by this quaternion.
    /// </summary>
    public Vector3 RotateVector(Vector3 vector)
    {
        var q = new Quaternion(vector, 0);
        var result = this * q * Conjugate;
        return new Vector3(result.X, result.Y, result.Z);
    }

    /// <summary>
    /// Converts to System.Numerics.Quaternion.
    /// </summary>
    public System.Numerics.Quaternion ToSystemQuaternion()
    {
        return new System.Numerics.Quaternion(X, Y, Z, W);
    }

    /// <summary>
    /// Creates from System.Numerics.Quaternion.
    /// </summary>
    public static Quaternion FromSystemQuaternion(System.Numerics.Quaternion quaternion)
    {
        return new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
    }

    // Operators
    public static Quaternion operator +(Quaternion left, Quaternion right)
    {
        return new Quaternion(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    public static Quaternion operator -(Quaternion left, Quaternion right)
    {
        return new Quaternion(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    public static Quaternion operator *(Quaternion left, Quaternion right)
    {
        return Multiply(left, right);
    }

    public static Quaternion operator *(Quaternion quaternion, float scalar)
    {
        return new Quaternion(quaternion.X * scalar, quaternion.Y * scalar, quaternion.Z * scalar, quaternion.W * scalar);
    }

    public static Quaternion operator *(float scalar, Quaternion quaternion)
    {
        return quaternion * scalar;
    }

    public static Quaternion operator -(Quaternion quaternion)
    {
        return new Quaternion(-quaternion.X, -quaternion.Y, -quaternion.Z, -quaternion.W);
    }

    public static bool operator ==(Quaternion left, Quaternion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Quaternion left, Quaternion right)
    {
        return !left.Equals(right);
    }

    public bool Equals(Quaternion other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
    }

    public override bool Equals(object? obj)
    {
        return obj is Quaternion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z, W);
    }

    public override string ToString()
    {
        return $"Quaternion({X:F3}, {Y:F3}, {Z:F3}, {W:F3})";
    }
}
