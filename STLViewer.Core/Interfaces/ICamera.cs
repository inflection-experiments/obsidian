using STLViewer.Math;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for 3D camera management.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets the camera position.
    /// </summary>
    Vector3 Position { get; }

    /// <summary>
    /// Gets the camera target position.
    /// </summary>
    Vector3 Target { get; }

    /// <summary>
    /// Gets the camera up vector.
    /// </summary>
    Vector3 Up { get; }

    /// <summary>
    /// Gets the field of view in radians.
    /// </summary>
    float FieldOfView { get; }

    /// <summary>
    /// Gets the aspect ratio.
    /// </summary>
    float AspectRatio { get; }

    /// <summary>
    /// Gets the near clipping plane distance.
    /// </summary>
    float NearPlane { get; }

    /// <summary>
    /// Gets the far clipping plane distance.
    /// </summary>
    float FarPlane { get; }

    /// <summary>
    /// Gets the view matrix.
    /// </summary>
    Matrix4x4 ViewMatrix { get; }

    /// <summary>
    /// Gets the projection matrix.
    /// </summary>
    Matrix4x4 ProjectionMatrix { get; }

    /// <summary>
    /// Gets the view-projection matrix.
    /// </summary>
    Matrix4x4 ViewProjectionMatrix { get; }

    /// <summary>
    /// Sets the camera position.
    /// </summary>
    /// <param name="position">The new camera position.</param>
    void SetPosition(Vector3 position);

    /// <summary>
    /// Sets the camera target.
    /// </summary>
    /// <param name="target">The new camera target.</param>
    void SetTarget(Vector3 target);

    /// <summary>
    /// Sets the camera up vector.
    /// </summary>
    /// <param name="up">The new camera up vector.</param>
    void SetUp(Vector3 up);

    /// <summary>
    /// Sets the field of view.
    /// </summary>
    /// <param name="fov">The field of view in radians.</param>
    void SetFieldOfView(float fov);

    /// <summary>
    /// Sets the aspect ratio.
    /// </summary>
    /// <param name="aspectRatio">The aspect ratio.</param>
    void SetAspectRatio(float aspectRatio);

    /// <summary>
    /// Sets the clipping planes.
    /// </summary>
    /// <param name="nearPlane">The near clipping plane distance.</param>
    /// <param name="farPlane">The far clipping plane distance.</param>
    void SetClippingPlanes(float nearPlane, float farPlane);

    /// <summary>
    /// Moves the camera to look at a target.
    /// </summary>
    /// <param name="position">The camera position.</param>
    /// <param name="target">The target to look at.</param>
    /// <param name="up">The up vector.</param>
    void LookAt(Vector3 position, Vector3 target, Vector3 up);

    /// <summary>
    /// Orbits the camera around a target.
    /// </summary>
    /// <param name="deltaX">The horizontal rotation delta.</param>
    /// <param name="deltaY">The vertical rotation delta.</param>
    void Orbit(float deltaX, float deltaY);

    /// <summary>
    /// Pans the camera.
    /// </summary>
    /// <param name="deltaX">The horizontal pan delta.</param>
    /// <param name="deltaY">The vertical pan delta.</param>
    void Pan(float deltaX, float deltaY);

    /// <summary>
    /// Zooms the camera.
    /// </summary>
    /// <param name="delta">The zoom delta.</param>
    void Zoom(float delta);

    /// <summary>
    /// Resets the camera to default settings.
    /// </summary>
    void Reset();

    /// <summary>
    /// Frames the camera to fit the specified bounding box.
    /// </summary>
    /// <param name="boundingBox">The bounding box to frame.</param>
    void Frame(BoundingBox boundingBox);

    /// <summary>
    /// Converts screen coordinates to world ray.
    /// </summary>
    /// <param name="screenX">The screen X coordinate.</param>
    /// <param name="screenY">The screen Y coordinate.</param>
    /// <param name="screenWidth">The screen width.</param>
    /// <param name="screenHeight">The screen height.</param>
    /// <returns>A ray from the camera through the screen point.</returns>
    Ray ScreenToWorldRay(float screenX, float screenY, float screenWidth, float screenHeight);

    /// <summary>
    /// Converts world coordinates to screen coordinates.
    /// </summary>
    /// <param name="worldPosition">The world position.</param>
    /// <param name="screenWidth">The screen width.</param>
    /// <param name="screenHeight">The screen height.</param>
    /// <returns>The screen coordinates.</returns>
    Vector3 WorldToScreen(Vector3 worldPosition, float screenWidth, float screenHeight);

    /// <summary>
    /// Sets the perspective projection parameters.
    /// </summary>
    /// <param name="fov">The field of view in degrees.</param>
    /// <param name="aspectRatio">The aspect ratio.</param>
    /// <param name="nearPlane">The near clipping plane distance.</param>
    /// <param name="farPlane">The far clipping plane distance.</param>
    void SetPerspective(float fov, float aspectRatio, float nearPlane, float farPlane);

    /// <summary>
    /// Frames the camera to bounds with optional padding.
    /// </summary>
    /// <param name="bounds">The bounding box to frame.</param>
    /// <param name="padding">Optional padding factor (default 1.2).</param>
    void FrameToBounds(BoundingBox bounds, float padding = 1.2f);
}
