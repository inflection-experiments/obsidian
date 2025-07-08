using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.Enums;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Infrastructure.Services;

/// <summary>
/// Implementation of measurement service for STL models.
/// </summary>
public class MeasurementService : IMeasurementService
{
    /// <summary>
    /// Measures the distance between two points in 3D space.
    /// </summary>
    public Result<DistanceMeasurement> MeasureDistance(Vector3 point1, Vector3 point2, string unit = "units")
    {
        try
        {
            var validation = ValidateMeasurementPoints(new[] { point1, point2 }, MeasurementType.Distance);
            if (!validation.IsSuccess)
                return Result<DistanceMeasurement>.Fail(validation.Error);

            var measurement = DistanceMeasurement.Create(point1, point2, unit);
            return Result.Ok(measurement);
        }
        catch (Exception ex)
        {
            return Result<DistanceMeasurement>.Fail($"Failed to measure distance: {ex.Message}");
        }
    }

    /// <summary>
    /// Measures the angle between three points (vertex and two endpoints).
    /// </summary>
    public Result<AngleMeasurement> MeasureAngle(Vector3 vertex, Vector3 point1, Vector3 point2)
    {
        try
        {
            var validation = ValidateMeasurementPoints(new[] { vertex, point1, point2 }, MeasurementType.Angle);
            if (!validation.IsSuccess)
                return Result<AngleMeasurement>.Fail(validation.Error);

            var measurement = AngleMeasurement.Create(vertex, point1, point2);
            return Result.Ok(measurement);
        }
        catch (Exception ex)
        {
            return Result<AngleMeasurement>.Fail($"Failed to measure angle: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates the volume of an STL model.
    /// </summary>
    public async Task<Result<VolumeMeasurement>> CalculateVolumeAsync(STLModel model, string unit = "cubic units", CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<VolumeMeasurement>.Fail("Model cannot be null");

            if (model.TriangleCount == 0)
                return Result<VolumeMeasurement>.Fail("Model has no triangles");

            // Check if the mesh is closed
            var closedMeshResult = await IsClosedMeshAsync(model, cancellationToken);
            if (!closedMeshResult.IsSuccess)
                return Result<VolumeMeasurement>.Fail($"Failed to check mesh closure: {closedMeshResult.Error}");

            var isClosedMesh = closedMeshResult.Value;
            var confidence = isClosedMesh ? 1.0f : 0.7f; // Lower confidence for non-closed meshes

            // Calculate volume using divergence theorem (signed volume)
            var volume = await CalculateSignedVolumeAsync(model, cancellationToken);

            // Take absolute value for final volume
            volume = MathF.Abs(volume);

            var measurement = new VolumeMeasurement
            {
                Volume = volume,
                Unit = unit,
                IsClosedMesh = isClosedMesh,
                Confidence = confidence
            };

            return Result.Ok(measurement);
        }
        catch (OperationCanceledException)
        {
            return Result<VolumeMeasurement>.Fail("Volume calculation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<VolumeMeasurement>.Fail($"Failed to calculate volume: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates the surface area of an STL model.
    /// </summary>
    public async Task<Result<SurfaceAreaMeasurement>> CalculateSurfaceAreaAsync(STLModel model, string unit = "square units", CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<SurfaceAreaMeasurement>.Fail("Model cannot be null");

            if (model.TriangleCount == 0)
                return Result<SurfaceAreaMeasurement>.Fail("Model has no triangles");

            // Calculate surface area by summing triangle areas
            var surfaceArea = 0.0f;
            var triangleCount = 0;

            await Task.Run(() =>
            {
                foreach (var triangle in model.Triangles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (triangle.IsValid && !triangle.IsDegenerate)
                    {
                        surfaceArea += triangle.Area;
                        triangleCount++;
                    }
                }
            }, cancellationToken);

            var measurement = new SurfaceAreaMeasurement
            {
                SurfaceArea = surfaceArea,
                Unit = unit,
                TriangleCount = triangleCount
            };

            return Result.Ok(measurement);
        }
        catch (OperationCanceledException)
        {
            return Result<SurfaceAreaMeasurement>.Fail("Surface area calculation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<SurfaceAreaMeasurement>.Fail($"Failed to calculate surface area: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets the bounding box measurements for an STL model.
    /// </summary>
    public Result<BoundingBoxMeasurement> GetBoundingBoxMeasurement(STLModel model, string unit = "units")
    {
        try
        {
            if (model == null)
                return Result<BoundingBoxMeasurement>.Fail("Model cannot be null");

            var measurement = new BoundingBoxMeasurement
            {
                BoundingBox = model.BoundingBox,
                Unit = unit
            };

            return Result.Ok(measurement);
        }
        catch (Exception ex)
        {
            return Result<BoundingBoxMeasurement>.Fail($"Failed to get bounding box measurement: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates the centroid (center of mass) of an STL model.
    /// </summary>
    public async Task<Result<CentroidMeasurement>> CalculateCentroidAsync(STLModel model, string method = "Geometric", string unit = "units", CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<CentroidMeasurement>.Fail("Model cannot be null");

            if (model.TriangleCount == 0)
                return Result<CentroidMeasurement>.Fail("Model has no triangles");

            var centroid = await Task.Run(() =>
            {
                return method.ToLowerInvariant() switch
                {
                    "geometric" => CalculateGeometricCentroid(model, cancellationToken),
                    "volumetric" => CalculateVolumetricCentroid(model, cancellationToken),
                    _ => CalculateGeometricCentroid(model, cancellationToken)
                };
            }, cancellationToken);

            var measurement = new CentroidMeasurement
            {
                Centroid = centroid,
                CalculationMethod = method,
                Unit = unit
            };

            return Result.Ok(measurement);
        }
        catch (OperationCanceledException)
        {
            return Result<CentroidMeasurement>.Fail("Centroid calculation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<CentroidMeasurement>.Fail($"Failed to calculate centroid: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the closest point on a model to a given point in space.
    /// </summary>
    public async Task<Result<Vector3>> FindClosestPointAsync(STLModel model, Vector3 targetPoint, CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<Vector3>.Fail("Model cannot be null");

            if (model.TriangleCount == 0)
                return Result<Vector3>.Fail("Model has no triangles");

            var closestPoint = Vector3.Zero;
            var minDistanceSquared = float.MaxValue;

            await Task.Run(() =>
            {
                foreach (var triangle in model.Triangles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!triangle.IsValid)
                        continue;

                    var pointOnTriangle = triangle.ClosestPoint(targetPoint);
                    var distanceSquared = Vector3.DistanceSquared(targetPoint, pointOnTriangle);

                    if (distanceSquared < minDistanceSquared)
                    {
                        minDistanceSquared = distanceSquared;
                        closestPoint = pointOnTriangle;
                    }
                }
            }, cancellationToken);

            return Result.Ok(closestPoint);
        }
        catch (OperationCanceledException)
        {
            return Result<Vector3>.Fail("Closest point search was cancelled");
        }
        catch (Exception ex)
        {
            return Result<Vector3>.Fail($"Failed to find closest point: {ex.Message}");
        }
    }

    /// <summary>
    /// Measures the distance from a point to the surface of a model.
    /// </summary>
    public async Task<Result<DistanceMeasurement>> MeasureDistanceToSurfaceAsync(STLModel model, Vector3 point, string unit = "units", CancellationToken cancellationToken = default)
    {
        try
        {
            var closestPointResult = await FindClosestPointAsync(model, point, cancellationToken);
            if (!closestPointResult.IsSuccess)
                return Result<DistanceMeasurement>.Fail($"Failed to find closest point: {closestPointResult.Error}");

            var closestPoint = closestPointResult.Value;
            var measurement = DistanceMeasurement.Create(point, closestPoint, unit);

            return Result.Ok(measurement);
        }
        catch (Exception ex)
        {
            return Result<DistanceMeasurement>.Fail($"Failed to measure distance to surface: {ex.Message}");
        }
    }

    /// <summary>
    /// Checks if a model represents a closed mesh suitable for volume calculations.
    /// </summary>
    public async Task<Result<bool>> IsClosedMeshAsync(STLModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<bool>.Fail("Model cannot be null");

            if (model.TriangleCount == 0)
                return Result.Ok(false);

            // Check for mesh closure by analyzing edge connectivity
            var edgeCount = new Dictionary<(Vector3, Vector3), int>();

            await Task.Run(() =>
            {
                foreach (var triangle in model.Triangles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!triangle.IsValid)
                        continue;

                    // Add edges (ensure consistent ordering)
                    AddEdge(edgeCount, triangle.Vertex1, triangle.Vertex2);
                    AddEdge(edgeCount, triangle.Vertex2, triangle.Vertex3);
                    AddEdge(edgeCount, triangle.Vertex3, triangle.Vertex1);
                }
            }, cancellationToken);

            // In a closed mesh, every edge should appear exactly twice
            var isClosed = edgeCount.Values.All(count => count == 2);

            return Result.Ok(isClosed);
        }
        catch (OperationCanceledException)
        {
            return Result<bool>.Fail("Mesh closure check was cancelled");
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to check mesh closure: {ex.Message}");
        }
    }

    /// <summary>
    /// Calculates various statistics about the model's mesh quality.
    /// </summary>
    public async Task<Result<Dictionary<string, object>>> CalculateMeshStatisticsAsync(STLModel model, CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<Dictionary<string, object>>.Fail("Model cannot be null");

            var statistics = new Dictionary<string, object>();

            await Task.Run(() =>
            {
                var validTriangles = 0;
                var degenerateTriangles = 0;
                var totalArea = 0.0f;
                var minArea = float.MaxValue;
                var maxArea = float.MinValue;
                var edgeLengths = new List<float>();

                foreach (var triangle in model.Triangles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (triangle.IsValid)
                    {
                        validTriangles++;

                        if (triangle.IsDegenerate)
                        {
                            degenerateTriangles++;
                        }
                        else
                        {
                            var area = triangle.Area;
                            totalArea += area;
                            minArea = MathF.Min(minArea, area);
                            maxArea = MathF.Max(maxArea, area);

                            // Collect edge lengths
                            edgeLengths.Add(Vector3.Distance(triangle.Vertex1, triangle.Vertex2));
                            edgeLengths.Add(Vector3.Distance(triangle.Vertex2, triangle.Vertex3));
                            edgeLengths.Add(Vector3.Distance(triangle.Vertex3, triangle.Vertex1));
                        }
                    }
                }

                statistics["TotalTriangles"] = model.TriangleCount;
                statistics["ValidTriangles"] = validTriangles;
                statistics["DegenerateTriangles"] = degenerateTriangles;
                statistics["InvalidTriangles"] = model.TriangleCount - validTriangles;
                statistics["TotalSurfaceArea"] = totalArea;
                statistics["MinTriangleArea"] = minArea == float.MaxValue ? 0.0f : minArea;
                statistics["MaxTriangleArea"] = maxArea == float.MinValue ? 0.0f : maxArea;
                statistics["AverageTriangleArea"] = validTriangles > 0 ? totalArea / validTriangles : 0.0f;

                if (edgeLengths.Count > 0)
                {
                    statistics["MinEdgeLength"] = edgeLengths.Min();
                    statistics["MaxEdgeLength"] = edgeLengths.Max();
                    statistics["AverageEdgeLength"] = edgeLengths.Average();
                    statistics["TotalEdges"] = edgeLengths.Count;
                }

                statistics["BoundingBoxVolume"] = model.BoundingBox.Volume;
                statistics["MeshQualityScore"] = CalculateMeshQualityScore(validTriangles, degenerateTriangles, model.TriangleCount);
            }, cancellationToken);

            return Result.Ok(statistics);
        }
        catch (OperationCanceledException)
        {
            return Result<Dictionary<string, object>>.Fail("Mesh statistics calculation was cancelled");
        }
        catch (Exception ex)
        {
            return Result<Dictionary<string, object>>.Fail($"Failed to calculate mesh statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// Performs a comprehensive analysis of the model including all measurements.
    /// </summary>
    public async Task<Result<MeasurementSession>> PerformComprehensiveAnalysisAsync(STLModel model, string unit = "units", CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result<MeasurementSession>.Fail("Model cannot be null");

            var measurements = new List<Measurement>();
            var session = MeasurementSession.Create($"Comprehensive Analysis - {model.Metadata?.FileName ?? "Unknown"}");

            // Bounding box measurement
            var boundingBoxResult = GetBoundingBoxMeasurement(model, unit);
            if (boundingBoxResult.IsSuccess)
                measurements.Add(boundingBoxResult.Value);

            // Surface area measurement
            var surfaceAreaResult = await CalculateSurfaceAreaAsync(model, $"square {unit}", cancellationToken);
            if (surfaceAreaResult.IsSuccess)
                measurements.Add(surfaceAreaResult.Value);

            // Volume measurement
            var volumeResult = await CalculateVolumeAsync(model, $"cubic {unit}", cancellationToken);
            if (volumeResult.IsSuccess)
                measurements.Add(volumeResult.Value);

            // Centroid measurement
            var centroidResult = await CalculateCentroidAsync(model, "Geometric", unit, cancellationToken);
            if (centroidResult.IsSuccess)
                measurements.Add(centroidResult.Value);

            session = session.AddMeasurements(measurements);
            return Result.Ok(session);
        }
        catch (OperationCanceledException)
        {
            return Result<MeasurementSession>.Fail("Comprehensive analysis was cancelled");
        }
        catch (Exception ex)
        {
            return Result<MeasurementSession>.Fail($"Failed to perform comprehensive analysis: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that measurement points are valid for the given operation.
    /// </summary>
    public Result<bool> ValidateMeasurementPoints(Vector3[] points, MeasurementType measurementType)
    {
        try
        {
            if (points == null)
                return Result<bool>.Fail("Points array cannot be null");

            // Check for finite values
            foreach (var point in points)
            {
                if (!IsFiniteVector(point))
                    return Result<bool>.Fail($"Point {point} contains invalid values (NaN or infinity)");
            }

            // Type-specific validation
            switch (measurementType)
            {
                case MeasurementType.Distance:
                    if (points.Length != 2)
                        return Result<bool>.Fail("Distance measurement requires exactly 2 points");

                    if (Vector3.DistanceSquared(points[0], points[1]) < float.Epsilon)
                        return Result<bool>.Fail("Points are too close together for meaningful distance measurement");
                    break;

                case MeasurementType.Angle:
                    if (points.Length != 3)
                        return Result<bool>.Fail("Angle measurement requires exactly 3 points");

                    // Check that we can form non-zero vectors
                    var vector1 = points[1] - points[0];
                    var vector2 = points[2] - points[0];

                    if (vector1.LengthSquared < float.Epsilon || vector2.LengthSquared < float.Epsilon)
                        return Result<bool>.Fail("Points are too close together to form a valid angle");
                    break;
            }

            return Result.Ok(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Failed to validate measurement points: {ex.Message}");
        }
    }

    /// <summary>
    /// Converts measurements between different units.
    /// </summary>
    public Result<Measurement> ConvertMeasurementUnits(Measurement measurement, string targetUnit, float conversionFactor)
    {
        try
        {
            if (measurement == null)
                return Result<Measurement>.Fail("Measurement cannot be null");

            if (conversionFactor <= 0)
                return Result<Measurement>.Fail("Conversion factor must be positive");

            if (string.IsNullOrWhiteSpace(targetUnit))
                return Result<Measurement>.Fail("Target unit cannot be null or empty");

            // Convert based on measurement type
            Measurement convertedMeasurement = measurement switch
            {
                DistanceMeasurement distance => distance with
                {
                    Distance = distance.Distance * conversionFactor,
                    Unit = targetUnit
                },
                VolumeMeasurement volume => volume with
                {
                    Volume = volume.Volume * (conversionFactor * conversionFactor * conversionFactor),
                    Unit = targetUnit
                },
                SurfaceAreaMeasurement surfaceArea => surfaceArea with
                {
                    SurfaceArea = surfaceArea.SurfaceArea * (conversionFactor * conversionFactor),
                    Unit = targetUnit
                },
                BoundingBoxMeasurement boundingBox => boundingBox with
                {
                    Unit = targetUnit,
                    BoundingBox = new BoundingBox(
                        boundingBox.BoundingBox.Min * conversionFactor,
                        boundingBox.BoundingBox.Max * conversionFactor)
                },
                CentroidMeasurement centroid => centroid with
                {
                    Centroid = centroid.Centroid * conversionFactor,
                    Unit = targetUnit
                },
                _ => throw new NotSupportedException($"Unit conversion not supported for {measurement.GetType().Name}")
            };

            return Result.Ok(convertedMeasurement);
        }
        catch (Exception ex)
        {
            return Result<Measurement>.Fail($"Failed to convert measurement units: {ex.Message}");
        }
    }

    #region Private Helper Methods

    private static async Task<float> CalculateSignedVolumeAsync(STLModel model, CancellationToken cancellationToken)
    {
        var volume = 0.0f;

        await Task.Run(() =>
        {
            foreach (var triangle in model.Triangles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!triangle.IsValid || triangle.IsDegenerate)
                    continue;

                // Calculate signed volume contribution using divergence theorem
                // V = (1/6) * Σ(v1 · (v2 × v3))
                var v1 = triangle.Vertex1;
                var v2 = triangle.Vertex2;
                var v3 = triangle.Vertex3;

                var cross = Vector3.Cross(v2, v3);
                var dot = Vector3.Dot(v1, cross);
                volume += dot;
            }
        }, cancellationToken);

        return volume / 6.0f;
    }

    private static Vector3 CalculateGeometricCentroid(STLModel model, CancellationToken cancellationToken)
    {
        var centroid = Vector3.Zero;
        var totalArea = 0.0f;

        foreach (var triangle in model.Triangles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!triangle.IsValid || triangle.IsDegenerate)
                continue;

            var area = triangle.Area;
            centroid += triangle.Centroid * area;
            totalArea += area;
        }

        return totalArea > 0 ? centroid / totalArea : Vector3.Zero;
    }

    private static Vector3 CalculateVolumetricCentroid(STLModel model, CancellationToken cancellationToken)
    {
        var centroid = Vector3.Zero;
        var totalVolume = 0.0f;

        foreach (var triangle in model.Triangles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!triangle.IsValid || triangle.IsDegenerate)
                continue;

            // Calculate signed volume of tetrahedron from origin to triangle
            var v1 = triangle.Vertex1;
            var v2 = triangle.Vertex2;
            var v3 = triangle.Vertex3;

            var cross = Vector3.Cross(v2, v3);
            var volume = Vector3.Dot(v1, cross) / 6.0f;

            // Centroid of tetrahedron from origin
            var tetrahedronCentroid = (v1 + v2 + v3) / 4.0f;

            centroid += tetrahedronCentroid * volume;
            totalVolume += volume;
        }

        return totalVolume != 0 ? centroid / totalVolume : Vector3.Zero;
    }

    private static void AddEdge(Dictionary<(Vector3, Vector3), int> edgeCount, Vector3 v1, Vector3 v2)
    {
        // Ensure consistent edge ordering
        var edge = v1.GetHashCode() < v2.GetHashCode() ? (v1, v2) : (v2, v1);

        if (edgeCount.ContainsKey(edge))
            edgeCount[edge]++;
        else
            edgeCount[edge] = 1;
    }

    private static bool IsFiniteVector(Vector3 vector)
    {
        return float.IsFinite(vector.X) && float.IsFinite(vector.Y) && float.IsFinite(vector.Z);
    }

    private static float CalculateMeshQualityScore(int validTriangles, int degenerateTriangles, int totalTriangles)
    {
        if (totalTriangles == 0)
            return 0.0f;

        var validRatio = (float)validTriangles / totalTriangles;
        var degenerateRatio = (float)degenerateTriangles / totalTriangles;

        // Quality score from 0 to 1
        return validRatio - (degenerateRatio * 0.5f);
    }

    #endregion
}
