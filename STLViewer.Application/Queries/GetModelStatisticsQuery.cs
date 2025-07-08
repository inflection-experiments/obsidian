using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Application.Commands;
using STLViewer.Application.DTOs;
using STLViewer.Domain.Common;
using STLViewer.Math;

namespace STLViewer.Application.Queries;

public class GetModelStatisticsQuery : IRequest<Result<ModelStatisticsDto>>
{
    public string FilePath { get; set; } = string.Empty;
    public bool CalculateVolume { get; set; } = true;
    public bool CalculateSurfaceArea { get; set; } = true;
    public bool CalculateBoundingBox { get; set; } = true;
}

public class ModelStatisticsDto
{
    public int TriangleCount { get; set; }
    public int VertexCount { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
    public float SurfaceArea { get; set; }
    public float Volume { get; set; }
    public Vector3Dto Centroid { get; set; } = new();
    public float MinEdgeLength { get; set; }
    public float MaxEdgeLength { get; set; }
    public float AverageEdgeLength { get; set; }
    public int DegenerateTriangleCount { get; set; }
    public bool IsManifold { get; set; }
    public bool IsWatertight { get; set; }
}

public class BoundingBoxDto
{
    public Vector3Dto Min { get; set; } = new();
    public Vector3Dto Max { get; set; } = new();
    public Vector3Dto Size { get; set; } = new();
    public Vector3Dto Center { get; set; } = new();
}

public class GetModelStatisticsQueryHandler : IRequestHandler<GetModelStatisticsQuery, Result<ModelStatisticsDto>>
{
    private readonly IMediator _mediator;
    private readonly ILogger<GetModelStatisticsQueryHandler> _logger;

    public GetModelStatisticsQueryHandler(
        IMediator mediator,
        ILogger<GetModelStatisticsQueryHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<ModelStatisticsDto>> Handle(GetModelStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Calculating statistics for STL model: {FilePath}", request.FilePath);

            // Load the model
            var loadCommand = new LoadSTLCommand
            {
                FilePath = request.FilePath,
                LoadTriangles = true,
                ValidateModel = false
            };

            var loadResult = await _mediator.Send(loadCommand, cancellationToken);
            if (loadResult.IsFailure)
                return Result<ModelStatisticsDto>.Fail(loadResult.Error);

            var model = loadResult.Value;
            var statistics = new ModelStatisticsDto
            {
                TriangleCount = model.Triangles.Count,
                VertexCount = model.Triangles.Count * 3 // Each triangle has 3 vertices
            };

            // Calculate bounding box
            if (request.CalculateBoundingBox && model.Triangles.Any())
            {
                statistics.BoundingBox = CalculateBoundingBox(model.Triangles);
            }

            // Calculate surface area
            if (request.CalculateSurfaceArea)
            {
                statistics.SurfaceArea = CalculateSurfaceArea(model.Triangles);
            }

            // Calculate volume
            if (request.CalculateVolume)
            {
                statistics.Volume = CalculateVolume(model.Triangles);
            }

            // Calculate other statistics
            CalculateEdgeStatistics(model.Triangles, statistics);
            CalculateGeometryStatistics(model.Triangles, statistics);

            _logger.LogInformation("Statistics calculated for model: {TriangleCount} triangles, {SurfaceArea:F2} surface area, {Volume:F2} volume",
                statistics.TriangleCount, statistics.SurfaceArea, statistics.Volume);

            return Result<ModelStatisticsDto>.Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating statistics for STL model: {FilePath}", request.FilePath);
            return Result<ModelStatisticsDto>.Fail($"Error calculating statistics: {ex.Message}");
        }
    }

    private BoundingBoxDto CalculateBoundingBox(List<TriangleDto> triangles)
    {
        var allVertices = triangles.SelectMany(t => new[] { t.Vertex1, t.Vertex2, t.Vertex3 });

        var minX = allVertices.Min(v => v.X);
        var minY = allVertices.Min(v => v.Y);
        var minZ = allVertices.Min(v => v.Z);
        var maxX = allVertices.Max(v => v.X);
        var maxY = allVertices.Max(v => v.Y);
        var maxZ = allVertices.Max(v => v.Z);

        var min = new Vector3Dto { X = minX, Y = minY, Z = minZ };
        var max = new Vector3Dto { X = maxX, Y = maxY, Z = maxZ };
        var size = new Vector3Dto { X = maxX - minX, Y = maxY - minY, Z = maxZ - minZ };
        var center = new Vector3Dto { X = (minX + maxX) / 2, Y = (minY + maxY) / 2, Z = (minZ + maxZ) / 2 };

        return new BoundingBoxDto
        {
            Min = min,
            Max = max,
            Size = size,
            Center = center
        };
    }

    private float CalculateSurfaceArea(List<TriangleDto> triangles)
    {
        float totalArea = 0;
        foreach (var triangle in triangles)
        {
            var v1 = ToVector3(triangle.Vertex1);
            var v2 = ToVector3(triangle.Vertex2);
            var v3 = ToVector3(triangle.Vertex3);

            var edge1 = v2 - v1;
            var edge2 = v3 - v1;
            var cross = Vector3.Cross(edge1, edge2);
            totalArea += cross.Length * 0.5f;
        }
        return totalArea;
    }

    private float CalculateVolume(List<TriangleDto> triangles)
    {
        float volume = 0;
        foreach (var triangle in triangles)
        {
            var v1 = ToVector3(triangle.Vertex1);
            var v2 = ToVector3(triangle.Vertex2);
            var v3 = ToVector3(triangle.Vertex3);

            // Calculate signed volume of tetrahedron formed by origin and triangle
            volume += Vector3.Dot(v1, Vector3.Cross(v2, v3)) / 6.0f;
        }
        return System.Math.Abs(volume);
    }

    private void CalculateEdgeStatistics(List<TriangleDto> triangles, ModelStatisticsDto statistics)
    {
        if (!triangles.Any()) return;

        var edgeLengths = new List<float>();
        foreach (var triangle in triangles)
        {
            var v1 = ToVector3(triangle.Vertex1);
            var v2 = ToVector3(triangle.Vertex2);
            var v3 = ToVector3(triangle.Vertex3);

            edgeLengths.Add((v2 - v1).Length);
            edgeLengths.Add((v3 - v2).Length);
            edgeLengths.Add((v1 - v3).Length);
        }

        statistics.MinEdgeLength = edgeLengths.Min();
        statistics.MaxEdgeLength = edgeLengths.Max();
        statistics.AverageEdgeLength = edgeLengths.Average();
    }

    private void CalculateGeometryStatistics(List<TriangleDto> triangles, ModelStatisticsDto statistics)
    {
        // Count degenerate triangles
        statistics.DegenerateTriangleCount = triangles.Count(t => IsDegenerateTriangle(t));

        // Calculate centroid
        var allVertices = triangles.SelectMany(t => new[] { t.Vertex1, t.Vertex2, t.Vertex3 });
        var centroid = new Vector3Dto
        {
            X = allVertices.Average(v => v.X),
            Y = allVertices.Average(v => v.Y),
            Z = allVertices.Average(v => v.Z)
        };
        statistics.Centroid = centroid;

        // Basic manifold check (simplified - a proper check would be more complex)
        statistics.IsManifold = statistics.DegenerateTriangleCount == 0;
        statistics.IsWatertight = statistics.IsManifold; // Simplified check
    }

    private bool IsDegenerateTriangle(TriangleDto triangle)
    {
        var v1 = ToVector3(triangle.Vertex1);
        var v2 = ToVector3(triangle.Vertex2);
        var v3 = ToVector3(triangle.Vertex3);

        var edge1 = v2 - v1;
        var edge2 = v3 - v1;
        var cross = Vector3.Cross(edge1, edge2);
        return cross.Length < 1e-6f;
    }

    private Vector3 ToVector3(Vector3Dto dto)
    {
        return new Vector3(dto.X, dto.Y, dto.Z);
    }
}
