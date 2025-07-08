namespace STLViewer.Domain.Enums;

/// <summary>
/// Defines the types of measurements that can be performed on STL models.
/// </summary>
public enum MeasurementType
{
    /// <summary>
    /// Measures the linear distance between two points.
    /// </summary>
    Distance,

    /// <summary>
    /// Measures the angle between two vectors or three points.
    /// </summary>
    Angle,

    /// <summary>
    /// Calculates the volume of a closed mesh.
    /// </summary>
    Volume,

    /// <summary>
    /// Calculates the total surface area of the mesh.
    /// </summary>
    SurfaceArea,

    /// <summary>
    /// Displays the bounding box dimensions and properties.
    /// </summary>
    BoundingBox,

    /// <summary>
    /// Measures the perimeter of a selected region or edge loop.
    /// </summary>
    Perimeter,

    /// <summary>
    /// Calculates the center of mass/centroid of the model.
    /// </summary>
    Centroid
}
