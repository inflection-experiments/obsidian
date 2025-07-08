using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents a predefined camera position and orientation.
/// </summary>
public class CameraPreset
{
    /// <summary>
    /// Gets the unique identifier for this camera preset.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the display name for this camera preset.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the camera position in world space.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Gets the camera target position (look-at point).
    /// </summary>
    public Vector3 Target { get; }

    /// <summary>
    /// Gets the up vector for the camera orientation.
    /// </summary>
    public Vector3 Up { get; }

    /// <summary>
    /// Gets the field of view in degrees.
    /// </summary>
    public float FieldOfView { get; }

    /// <summary>
    /// Gets the description of this camera preset.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the keyboard shortcut for this preset.
    /// </summary>
    public string? KeyboardShortcut { get; }

    /// <summary>
    /// Initializes a new instance of the CameraPreset class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="displayName">The display name.</param>
    /// <param name="position">The camera position.</param>
    /// <param name="target">The camera target position.</param>
    /// <param name="up">The up vector.</param>
    /// <param name="fieldOfView">The field of view in degrees.</param>
    /// <param name="description">The description.</param>
    /// <param name="keyboardShortcut">The keyboard shortcut (optional).</param>
    public CameraPreset(
        string id,
        string displayName,
        Vector3 position,
        Vector3 target,
        Vector3 up,
        float fieldOfView = 45.0f,
        string description = "",
        string? keyboardShortcut = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Position = position;
        Target = target;
        Up = up;
        FieldOfView = fieldOfView;
        Description = description;
        KeyboardShortcut = keyboardShortcut;
    }

    /// <summary>
    /// Creates a preset for the front view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A front view camera preset.</returns>
    public static CameraPreset CreateFrontView(float distance = 10.0f)
    {
        return new CameraPreset(
            "front",
            "Front View",
            new Vector3(0, 0, distance),
            Vector3.Zero,
            Vector3.UnitY,
            45.0f,
            "View from the front along the Z-axis",
            "F1"
        );
    }

    /// <summary>
    /// Creates a preset for the back view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A back view camera preset.</returns>
    public static CameraPreset CreateBackView(float distance = 10.0f)
    {
        return new CameraPreset(
            "back",
            "Back View",
            new Vector3(0, 0, -distance),
            Vector3.Zero,
            Vector3.UnitY,
            45.0f,
            "View from the back along the Z-axis",
            "F2"
        );
    }

    /// <summary>
    /// Creates a preset for the top view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A top view camera preset.</returns>
    public static CameraPreset CreateTopView(float distance = 10.0f)
    {
        return new CameraPreset(
            "top",
            "Top View",
            new Vector3(0, distance, 0),
            Vector3.Zero,
            new Vector3(0, 0, -1), // Z-up in top view
            45.0f,
            "View from above along the Y-axis",
            "F3"
        );
    }

    /// <summary>
    /// Creates a preset for the bottom view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A bottom view camera preset.</returns>
    public static CameraPreset CreateBottomView(float distance = 10.0f)
    {
        return new CameraPreset(
            "bottom",
            "Bottom View",
            new Vector3(0, -distance, 0),
            Vector3.Zero,
            new Vector3(0, 0, 1), // Z-up in bottom view
            45.0f,
            "View from below along the Y-axis",
            "F4"
        );
    }

    /// <summary>
    /// Creates a preset for the left view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A left view camera preset.</returns>
    public static CameraPreset CreateLeftView(float distance = 10.0f)
    {
        return new CameraPreset(
            "left",
            "Left View",
            new Vector3(-distance, 0, 0),
            Vector3.Zero,
            Vector3.UnitY,
            45.0f,
            "View from the left along the X-axis",
            "F5"
        );
    }

    /// <summary>
    /// Creates a preset for the right view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A right view camera preset.</returns>
    public static CameraPreset CreateRightView(float distance = 10.0f)
    {
        return new CameraPreset(
            "right",
            "Right View",
            new Vector3(distance, 0, 0),
            Vector3.Zero,
            Vector3.UnitY,
            45.0f,
            "View from the right along the X-axis",
            "F6"
        );
    }

    /// <summary>
    /// Creates a preset for the isometric view.
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>An isometric view camera preset.</returns>
    public static CameraPreset CreateIsometricView(float distance = 10.0f)
    {
        var position = new Vector3(distance * 0.707f, distance * 0.707f, distance * 0.707f);
        return new CameraPreset(
            "isometric",
            "Isometric View",
            position,
            Vector3.Zero,
            Vector3.UnitY,
            45.0f,
            "3D isometric view showing all three axes",
            "F7"
        );
    }

    /// <summary>
    /// Creates a preset for the perspective view (default 3D view).
    /// </summary>
    /// <param name="distance">The distance from the target.</param>
    /// <returns>A perspective view camera preset.</returns>
    public static CameraPreset CreatePerspectiveView(float distance = 10.0f)
    {
        var position = new Vector3(distance * 0.5f, distance * 0.3f, distance * 0.8f);
        return new CameraPreset(
            "perspective",
            "Perspective View",
            position,
            Vector3.Zero,
            Vector3.UnitY,
            45.0f,
            "Default 3D perspective view",
            "F8"
        );
    }

    /// <summary>
    /// Gets all standard camera presets.
    /// </summary>
    /// <param name="distance">The distance from the target for all presets.</param>
    /// <returns>A collection of standard camera presets.</returns>
    public static IEnumerable<CameraPreset> GetStandardPresets(float distance = 10.0f)
    {
        yield return CreateFrontView(distance);
        yield return CreateBackView(distance);
        yield return CreateTopView(distance);
        yield return CreateBottomView(distance);
        yield return CreateLeftView(distance);
        yield return CreateRightView(distance);
        yield return CreateIsometricView(distance);
        yield return CreatePerspectiveView(distance);
    }

    /// <summary>
    /// Adjusts the camera preset for a specific bounding box.
    /// </summary>
    /// <param name="boundingBox">The bounding box to frame.</param>
    /// <returns>A new camera preset adjusted for the bounding box.</returns>
    public CameraPreset AdjustForBoundingBox(BoundingBox boundingBox)
    {
        var center = boundingBox.Center;
        var size = boundingBox.Size;
        var maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));

        // Calculate appropriate distance based on bounding box size and field of view
        var distance = maxDimension / (2.0f * MathF.Tan(FieldOfView * 0.5f * MathF.PI / 180.0f)) * 1.5f;

        // Calculate the direction from target to camera
        var direction = (Position - Target).Normalized();

        // Position camera at calculated distance from bounding box center
        var adjustedPosition = center + direction * distance;

        return new CameraPreset(
            Id,
            DisplayName,
            adjustedPosition,
            center,
            Up,
            FieldOfView,
            Description,
            KeyboardShortcut
        );
    }

    public override bool Equals(object? obj)
    {
        return obj is CameraPreset other && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"{DisplayName} ({Id})";
    }
}
