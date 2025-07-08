using STLViewer.Core.Interfaces;
using STLViewer.Math;

namespace STLViewer.Infrastructure.Graphics;

/// <summary>
/// A 3D camera implementation with orbit, pan, and zoom functionality.
/// </summary>
public class Camera : ICamera
{
    private Vector3 _position;
    private Vector3 _target;
    private Vector3 _up;
    private float _fieldOfView;
    private float _aspectRatio;
    private float _nearPlane;
    private float _farPlane;
    private bool _viewMatrixDirty = true;
    private bool _projectionMatrixDirty = true;
    private Matrix4x4 _viewMatrix;
    private Matrix4x4 _projectionMatrix;

    /// <summary>
    /// Initializes a new instance of the Camera class.
    /// </summary>
    public Camera()
    {
        Reset();
    }

    /// <summary>
    /// Initializes a new instance of the Camera class with specified parameters.
    /// </summary>
    /// <param name="position">The camera position.</param>
    /// <param name="target">The camera target.</param>
    /// <param name="up">The camera up vector.</param>
    /// <param name="fieldOfView">The field of view in radians.</param>
    /// <param name="aspectRatio">The aspect ratio.</param>
    /// <param name="nearPlane">The near clipping plane distance.</param>
    /// <param name="farPlane">The far clipping plane distance.</param>
    public Camera(Vector3 position, Vector3 target, Vector3 up, float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        _position = position;
        _target = target;
        _up = up;
        _fieldOfView = fieldOfView;
        _aspectRatio = aspectRatio;
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        _viewMatrixDirty = true;
        _projectionMatrixDirty = true;
    }

    /// <inheritdoc/>
    public Vector3 Position => _position;

    /// <inheritdoc/>
    public Vector3 Target => _target;

    /// <inheritdoc/>
    public Vector3 Up => _up;

    /// <inheritdoc/>
    public float FieldOfView => _fieldOfView;

    /// <inheritdoc/>
    public float AspectRatio => _aspectRatio;

    /// <inheritdoc/>
    public float NearPlane => _nearPlane;

    /// <inheritdoc/>
    public float FarPlane => _farPlane;

    /// <inheritdoc/>
    public Matrix4x4 ViewMatrix
    {
        get
        {
            if (_viewMatrixDirty)
            {
                _viewMatrix = Matrix4x4.CreateLookAt(_position, _target, _up);
                _viewMatrixDirty = false;
            }
            return _viewMatrix;
        }
    }

    /// <inheritdoc/>
    public Matrix4x4 ProjectionMatrix
    {
        get
        {
            if (_projectionMatrixDirty)
            {
                _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(_fieldOfView, _aspectRatio, _nearPlane, _farPlane);
                _projectionMatrixDirty = false;
            }
            return _projectionMatrix;
        }
    }

    /// <inheritdoc/>
    public Matrix4x4 ViewProjectionMatrix => ViewMatrix * ProjectionMatrix;

    /// <inheritdoc/>
    public void SetPosition(Vector3 position)
    {
        _position = position;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void SetTarget(Vector3 target)
    {
        _target = target;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void SetUp(Vector3 up)
    {
        _up = up;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void SetFieldOfView(float fov)
    {
        _fieldOfView = fov;
        _projectionMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void SetAspectRatio(float aspectRatio)
    {
        _aspectRatio = aspectRatio;
        _projectionMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void SetClippingPlanes(float nearPlane, float farPlane)
    {
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        _projectionMatrixDirty = true;
    }

    /// <summary>
    /// Sets perspective projection parameters in a single call.
    /// </summary>
    /// <param name="fieldOfView">Field of view in degrees.</param>
    /// <param name="aspectRatio">Aspect ratio.</param>
    /// <param name="nearPlane">Near clipping plane.</param>
    /// <param name="farPlane">Far clipping plane.</param>
    public void SetPerspective(float fieldOfView, float aspectRatio, float nearPlane, float farPlane)
    {
        _fieldOfView = fieldOfView * MathF.PI / 180.0f; // Convert degrees to radians
        _aspectRatio = aspectRatio;
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        _projectionMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void LookAt(Vector3 position, Vector3 target, Vector3 up)
    {
        _position = position;
        _target = target;
        _up = up;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void Orbit(float deltaX, float deltaY)
    {
        var toTarget = _target - _position;
        var distance = toTarget.Length;

        if (distance < float.Epsilon)
            return;

        // Create spherical coordinates
        var theta = MathF.Atan2(toTarget.X, toTarget.Z);
        var phi = MathF.Acos(toTarget.Y / distance);

        // Apply rotation deltas
        theta += deltaX;
        phi += deltaY;

        // Clamp phi to avoid gimbal lock
        phi = System.Math.Clamp(phi, 0.1f, MathF.PI - 0.1f);

        // Convert back to Cartesian coordinates
        var newToTarget = new Vector3(
            distance * MathF.Sin(phi) * MathF.Sin(theta),
            distance * MathF.Cos(phi),
            distance * MathF.Sin(phi) * MathF.Cos(theta)
        );

        _position = _target - newToTarget;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void Pan(float deltaX, float deltaY)
    {
        var forward = (_target - _position).Normalized();
        var right = Vector3.Cross(forward, _up).Normalized();
        var up = Vector3.Cross(right, forward).Normalized();

        var panOffset = right * deltaX + up * deltaY;
        _position += panOffset;
        _target += panOffset;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void Zoom(float delta)
    {
        var toTarget = _target - _position;
        var distance = toTarget.Length;

        if (distance < float.Epsilon)
            return;

        var newDistance = distance * (1.0f + delta);
        newDistance = System.Math.Max(newDistance, 0.1f); // Minimum distance

        var direction = toTarget.Normalized();
        _position = _target - direction * newDistance;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _position = new Vector3(0, 0, 5);
        _target = Vector3.Zero;
        _up = Vector3.UnitY;
        _fieldOfView = MathF.PI / 4; // 45 degrees
        _aspectRatio = 16.0f / 9.0f;
        _nearPlane = 0.1f;
        _farPlane = 1000.0f;
        _viewMatrixDirty = true;
        _projectionMatrixDirty = true;
    }

    /// <inheritdoc/>
    public void Frame(BoundingBox boundingBox)
    {
        var center = boundingBox.Center;
        var size = boundingBox.Size;
        var maxDimension = System.Math.Max(size.X, System.Math.Max(size.Y, size.Z));

        // Calculate distance needed to fit the bounding box
        var distance = maxDimension / (2.0f * MathF.Tan(_fieldOfView * 0.5f));
        distance *= 1.5f; // Add some padding

        // Position camera to look at the center
        var direction = (_position - _target).Normalized();
        if (direction.LengthSquared < float.Epsilon)
            direction = new Vector3(0, 0, 1);

        _target = center;
        _position = center + direction * distance;
        _viewMatrixDirty = true;
    }

    /// <summary>
    /// Frames the camera to show the entire bounding box with optional padding.
    /// </summary>
    /// <param name="boundingBox">The bounding box to frame.</param>
    /// <param name="padding">Optional padding factor (default 1.2).</param>
    public void FrameToBounds(BoundingBox boundingBox, float padding = 1.2f)
    {
        var center = boundingBox.Center;
        var size = boundingBox.Size;
        var maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));

        if (maxDimension < float.Epsilon)
            return;

        // Calculate distance needed to fit the bounding box
        var distance = maxDimension / (2.0f * MathF.Tan(_fieldOfView * 0.5f));
        distance *= padding; // Apply padding

        // Position camera at distance from center
        var direction = (_position - _target).Normalized();
        if (direction.LengthSquared < float.Epsilon)
            direction = Vector3.UnitZ;

        _position = center + direction * distance;
        _target = center;
        _viewMatrixDirty = true;
    }

    /// <inheritdoc/>
    public Ray ScreenToWorldRay(float screenX, float screenY, float screenWidth, float screenHeight)
    {
        // Convert screen coordinates to normalized device coordinates
        var x = (2.0f * screenX) / screenWidth - 1.0f;
        var y = 1.0f - (2.0f * screenY) / screenHeight;

        // Create points in clip space
        var nearPoint = new Vector3(x, y, -1.0f);
        var farPoint = new Vector3(x, y, 1.0f);

        // Convert to world space
        var inverseViewProjection = ViewProjectionMatrix.Inverse();
        var worldNear = inverseViewProjection.TransformPoint(nearPoint);
        var worldFar = inverseViewProjection.TransformPoint(farPoint);

        // Create ray
        var direction = (worldFar - worldNear).Normalized();
        return new Ray(worldNear, direction);
    }

    /// <inheritdoc/>
    public Vector3 WorldToScreen(Vector3 worldPosition, float screenWidth, float screenHeight)
    {
        var clipSpace = ViewProjectionMatrix.TransformPoint(worldPosition);

        // Perspective divide
        if (MathF.Abs(clipSpace.Z) > float.Epsilon)
        {
            clipSpace = new Vector3(clipSpace.X / clipSpace.Z, clipSpace.Y / clipSpace.Z, clipSpace.Z);
        }

        // Convert to screen coordinates
        var screenX = (clipSpace.X + 1.0f) * 0.5f * screenWidth;
        var screenY = (1.0f - clipSpace.Y) * 0.5f * screenHeight;

        return new Vector3(screenX, screenY, clipSpace.Z);
    }
}
