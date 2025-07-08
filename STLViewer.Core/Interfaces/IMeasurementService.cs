using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.Enums;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for performing measurements on STL models.
/// </summary>
public interface IMeasurementService
{
    /// <summary>
    /// Measures the distance between two points in 3D space.
    /// </summary>
    /// <param name="point1">The first point.</param>
    /// <param name="point2">The second point.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <returns>A distance measurement result.</returns>
    Result<DistanceMeasurement> MeasureDistance(Vector3 point1, Vector3 point2, string unit = "units");

    /// <summary>
    /// Measures the angle between three points (vertex and two endpoints).
    /// </summary>
    /// <param name="vertex">The vertex point where the angle is measured.</param>
    /// <param name="point1">The first endpoint.</param>
    /// <param name="point2">The second endpoint.</param>
    /// <returns>An angle measurement result.</returns>
    Result<AngleMeasurement> MeasureAngle(Vector3 vertex, Vector3 point1, Vector3 point2);

    /// <summary>
    /// Calculates the volume of an STL model.
    /// </summary>
    /// <param name="model">The STL model to measure.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A volume measurement result.</returns>
    Task<Result<VolumeMeasurement>> CalculateVolumeAsync(STLModel model, string unit = "cubic units", CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the surface area of an STL model.
    /// </summary>
    /// <param name="model">The STL model to measure.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A surface area measurement result.</returns>
    Task<Result<SurfaceAreaMeasurement>> CalculateSurfaceAreaAsync(STLModel model, string unit = "square units", CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the bounding box measurements for an STL model.
    /// </summary>
    /// <param name="model">The STL model to measure.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <returns>A bounding box measurement result.</returns>
    Result<BoundingBoxMeasurement> GetBoundingBoxMeasurement(STLModel model, string unit = "units");

    /// <summary>
    /// Calculates the centroid (center of mass) of an STL model.
    /// </summary>
    /// <param name="model">The STL model to measure.</param>
    /// <param name="method">The calculation method to use (e.g., "Geometric", "Volumetric").</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A centroid measurement result.</returns>
    Task<Result<CentroidMeasurement>> CalculateCentroidAsync(STLModel model, string method = "Geometric", string unit = "units", CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the closest point on a model to a given point in space.
    /// </summary>
    /// <param name="model">The STL model to search.</param>
    /// <param name="targetPoint">The point to find the closest point to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The closest point on the model surface.</returns>
    Task<Result<Vector3>> FindClosestPointAsync(STLModel model, Vector3 targetPoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Measures the distance from a point to the surface of a model.
    /// </summary>
    /// <param name="model">The STL model.</param>
    /// <param name="point">The point to measure from.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A distance measurement to the closest surface point.</returns>
    Task<Result<DistanceMeasurement>> MeasureDistanceToSurfaceAsync(STLModel model, Vector3 point, string unit = "units", CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model represents a closed mesh suitable for volume calculations.
    /// </summary>
    /// <param name="model">The STL model to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the mesh is closed; otherwise, false.</returns>
    Task<Result<bool>> IsClosedMeshAsync(STLModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates various statistics about the model's mesh quality.
    /// </summary>
    /// <param name="model">The STL model to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A dictionary containing mesh quality statistics.</returns>
    Task<Result<Dictionary<string, object>>> CalculateMeshStatisticsAsync(STLModel model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a comprehensive analysis of the model including all measurements.
    /// </summary>
    /// <param name="model">The STL model to analyze.</param>
    /// <param name="unit">The unit of measurement.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A measurement session containing all calculated measurements.</returns>
    Task<Result<MeasurementSession>> PerformComprehensiveAnalysisAsync(STLModel model, string unit = "units", CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that measurement points are valid for the given operation.
    /// </summary>
    /// <param name="points">The points to validate.</param>
    /// <param name="measurementType">The type of measurement being performed.</param>
    /// <returns>True if the points are valid; otherwise, false with error details.</returns>
    Result<bool> ValidateMeasurementPoints(Vector3[] points, MeasurementType measurementType);

    /// <summary>
    /// Converts measurements between different units.
    /// </summary>
    /// <param name="measurement">The measurement to convert.</param>
    /// <param name="targetUnit">The target unit to convert to.</param>
    /// <param name="conversionFactor">The conversion factor to apply.</param>
    /// <returns>A new measurement with converted values.</returns>
    Result<Measurement> ConvertMeasurementUnits(Measurement measurement, string targetUnit, float conversionFactor);
}
