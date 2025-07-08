using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents a 3D transformation including position, rotation, and scale.
/// </summary>
public sealed record Transform
{
    /// <summary>
    /// Gets the position/translation component.
    /// </summary>
    public Vector3 Position { get; init; } = Vector3.Zero;

    /// <summary>
    /// Gets the rotation component as a quaternion.
    /// </summary>
    public Quaternion Rotation { get; init; } = Quaternion.Identity;

    /// <summary>
    /// Gets the scale component.
    /// </summary>
    public Vector3 Scale { get; init; } = Vector3.One;

    /// <summary>
    /// Gets the transformation matrix representation.
    /// </summary>
    public Matrix4x4 Matrix
    {
        get
        {
            var scaleMatrix = Matrix4x4.CreateScale(Scale);
            var rotationMatrix = Rotation.ToRotationMatrix();
            var translationMatrix = Matrix4x4.CreateTranslation(Position);

            return scaleMatrix * rotationMatrix * translationMatrix;
        }
    }

    /// <summary>
    /// Gets the identity transform (no transformation applied).
    /// </summary>
    public static Transform Identity => new();

    /// <summary>
    /// Gets a value indicating whether this transform represents the identity transformation.
    /// </summary>
    public bool IsIdentity =>
        Position == Vector3.Zero &&
        Rotation == Quaternion.Identity &&
        Scale == Vector3.One;

    /// <summary>
    /// Gets the forward direction vector in world space.
    /// </summary>
    public Vector3 Forward => Rotation.RotateVector(new Vector3(0, 0, -1));

    /// <summary>
    /// Gets the right direction vector in world space.
    /// </summary>
    public Vector3 Right => Rotation.RotateVector(new Vector3(1, 0, 0));

    /// <summary>
    /// Gets the up direction vector in world space.
    /// </summary>
    public Vector3 Up => Rotation.RotateVector(new Vector3(0, 1, 0));

    private Transform() { }

    /// <summary>
    /// Creates a new transform with the specified components.
    /// </summary>
    /// <param name="position">The position/translation.</param>
    /// <param name="rotation">The rotation quaternion.</param>
    /// <param name="scale">The scale factor.</param>
    /// <returns>A new transform instance.</returns>
    public static Transform Create(Vector3? position = null, Quaternion? rotation = null, Vector3? scale = null)
    {
        return new Transform
        {
            Position = position ?? Vector3.Zero,
            Rotation = rotation ?? Quaternion.Identity,
            Scale = scale ?? Vector3.One
        };
    }

    /// <summary>
    /// Creates a transform with only a position.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <returns>A new transform instance.</returns>
    public static Transform CreateTranslation(Vector3 position)
    {
        return new Transform { Position = position };
    }

    /// <summary>
    /// Creates a transform with only a rotation.
    /// </summary>
    /// <param name="rotation">The rotation quaternion.</param>
    /// <returns>A new transform instance.</returns>
    public static Transform CreateRotation(Quaternion rotation)
    {
        return new Transform { Rotation = rotation };
    }

    /// <summary>
    /// Creates a transform with only a scale.
    /// </summary>
    /// <param name="scale">The scale factor.</param>
    /// <returns>A new transform instance.</returns>
    public static Transform CreateScale(Vector3 scale)
    {
        return new Transform { Scale = scale };
    }

    /// <summary>
    /// Creates a transform with uniform scale.
    /// </summary>
    /// <param name="uniformScale">The uniform scale factor.</param>
    /// <returns>A new transform instance.</returns>
    public static Transform CreateUniformScale(float uniformScale)
    {
        return new Transform { Scale = new Vector3(uniformScale) };
    }

    /// <summary>
    /// Creates a transform from Euler angles.
    /// </summary>
    /// <param name="eulerAngles">The Euler angles in radians (pitch, yaw, roll).</param>
    /// <returns>A new transform instance.</returns>
    public static Transform CreateFromEulerAngles(Vector3 eulerAngles)
    {
        return new Transform { Rotation = Quaternion.CreateFromEuler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z) };
    }

    /// <summary>
    /// Creates a transform from a transformation matrix.
    /// </summary>
    /// <param name="matrix">The transformation matrix.</param>
    /// <returns>A new transform instance.</returns>
    public static Transform CreateFromMatrix(Matrix4x4 matrix)
    {
        // For simplicity, we'll extract only the translation component
        // In a full implementation, you'd need to decompose the matrix properly
        var translation = new Vector3(matrix.M41, matrix.M42, matrix.M43);
        return new Transform
        {
            Position = translation,
            Rotation = Quaternion.Identity,
            Scale = Vector3.One
        };
    }

    /// <summary>
    /// Combines this transform with another transform (applies other transform after this one).
    /// </summary>
    /// <param name="other">The other transform to combine with.</param>
    /// <returns>A new combined transform.</returns>
    public Transform CombineWith(Transform other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        // Combine transformations: scale, then rotate, then translate
        var combinedScale = Scale * other.Scale;
        var combinedRotation = Rotation * other.Rotation;
        var combinedPosition = Position + other.Rotation.RotateVector(other.Position * Scale);

        return new Transform
        {
            Position = combinedPosition,
            Rotation = combinedRotation,
            Scale = combinedScale
        };
    }

    /// <summary>
    /// Gets the inverse of this transform.
    /// </summary>
    /// <returns>The inverse transform.</returns>
    public Transform Inverse()
    {
        var inverseRotation = Rotation.Inverse;
        var inverseScale = new Vector3(1.0f / Scale.X, 1.0f / Scale.Y, 1.0f / Scale.Z);
        var inversePosition = inverseRotation.RotateVector(-Position) * inverseScale;

        return new Transform
        {
            Position = inversePosition,
            Rotation = inverseRotation,
            Scale = inverseScale
        };
    }

    /// <summary>
    /// Transforms a point from local space to world space using this transform.
    /// </summary>
    /// <param name="point">The point in local space.</param>
    /// <returns>The point in world space.</returns>
    public Vector3 TransformPoint(Vector3 point)
    {
        // Apply scale, then rotation, then translation
        var scaled = point * Scale;
        var rotated = Rotation.RotateVector(scaled);
        return rotated + Position;
    }

    /// <summary>
    /// Transforms a direction vector from local space to world space using this transform.
    /// Note: Direction vectors are not affected by translation.
    /// </summary>
    /// <param name="direction">The direction in local space.</param>
    /// <returns>The direction in world space.</returns>
    public Vector3 TransformDirection(Vector3 direction)
    {
        // Apply scale and rotation, but not translation
        var scaled = direction * Scale;
        return Rotation.RotateVector(scaled);
    }

    /// <summary>
    /// Transforms a normal vector from local space to world space using this transform.
    /// Note: Normal vectors require special handling for non-uniform scaling.
    /// </summary>
    /// <param name="normal">The normal in local space.</param>
    /// <returns>The normal in world space.</returns>
    public Vector3 TransformNormal(Vector3 normal)
    {
        // For normals, we need to use the inverse transpose for non-uniform scaling
        var inverseScale = new Vector3(1.0f / Scale.X, 1.0f / Scale.Y, 1.0f / Scale.Z);
        var scaled = normal * inverseScale;
        var rotated = Rotation.RotateVector(scaled);
        return rotated.Normalized();
    }

    /// <summary>
    /// Creates a new transform with modified position.
    /// </summary>
    /// <param name="position">The new position.</param>
    /// <returns>A new transform with the specified position.</returns>
    public Transform WithPosition(Vector3 position)
    {
        return this with { Position = position };
    }

    /// <summary>
    /// Creates a new transform with modified rotation.
    /// </summary>
    /// <param name="rotation">The new rotation.</param>
    /// <returns>A new transform with the specified rotation.</returns>
    public Transform WithRotation(Quaternion rotation)
    {
        return this with { Rotation = rotation };
    }

    /// <summary>
    /// Creates a new transform with modified scale.
    /// </summary>
    /// <param name="scale">The new scale.</param>
    /// <returns>A new transform with the specified scale.</returns>
    public Transform WithScale(Vector3 scale)
    {
        return this with { Scale = scale };
    }

    /// <summary>
    /// Creates a new transform with a position offset applied.
    /// </summary>
    /// <param name="offset">The position offset to apply.</param>
    /// <returns>A new transform with the offset applied.</returns>
    public Transform Translate(Vector3 offset)
    {
        return this with { Position = Position + offset };
    }

    /// <summary>
    /// Creates a new transform with a rotation applied.
    /// </summary>
    /// <param name="rotation">The rotation to apply.</param>
    /// <returns>A new transform with the rotation applied.</returns>
    public Transform Rotate(Quaternion rotation)
    {
        return this with { Rotation = Rotation * rotation };
    }

    /// <summary>
    /// Creates a new transform with a scale factor applied.
    /// </summary>
    /// <param name="scaleFactor">The scale factor to apply.</param>
    /// <returns>A new transform with the scale applied.</returns>
    public Transform ScaleBy(Vector3 scaleFactor)
    {
        return this with { Scale = Scale * scaleFactor };
    }

    /// <summary>
    /// Creates a new transform with a uniform scale factor applied.
    /// </summary>
    /// <param name="uniformScale">The uniform scale factor to apply.</param>
    /// <returns>A new transform with the scale applied.</returns>
    public Transform ScaleBy(float uniformScale)
    {
        return ScaleBy(new Vector3(uniformScale));
    }
}
