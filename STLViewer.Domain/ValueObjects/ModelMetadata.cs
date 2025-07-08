using System;
using STLViewer.Domain.Enums;
using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents metadata information about an STL model.
/// </summary>
public sealed record ModelMetadata
{
    /// <summary>
    /// The original filename of the STL file.
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// The format of the STL file (ASCII or Binary).
    /// </summary>
    public STLFormat Format { get; init; }

    /// <summary>
    /// The number of triangles in the model.
    /// </summary>
    public int TriangleCount { get; init; }

    /// <summary>
    /// The total surface area of the model.
    /// </summary>
    public float SurfaceArea { get; init; }

    /// <summary>
    /// The volume of the model (if it's a closed mesh).
    /// </summary>
    public float? Volume { get; init; }

    /// <summary>
    /// The bounding box that encompasses the entire model.
    /// </summary>
    public BoundingBox BoundingBox { get; init; }

    /// <summary>
    /// The date and time when the file was created or last modified.
    /// </summary>
    public DateTime? LastModified { get; init; }

    /// <summary>
    /// The date and time when the model was loaded into the application.
    /// </summary>
    public DateTime LoadedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Any additional properties or comments from the STL file header.
    /// </summary>
    public Dictionary<string, string> AdditionalProperties { get; init; } = new();

    /// <summary>
    /// Indicates whether the model appears to be a closed mesh.
    /// </summary>
    public bool? IsClosedMesh { get; init; }

    /// <summary>
    /// The minimum edge length in the model.
    /// </summary>
    public float? MinEdgeLength { get; init; }

    /// <summary>
    /// The maximum edge length in the model.
    /// </summary>
    public float? MaxEdgeLength { get; init; }

    /// <summary>
    /// The average edge length in the model.
    /// </summary>
    public float? AverageEdgeLength { get; init; }

    /// <summary>
    /// The number of degenerate triangles in the model.
    /// </summary>
    public int DegenerateTriangleCount { get; init; }

    /// <summary>
    /// A hash or checksum of the file content for integrity checking.
    /// </summary>
    public string? ContentHash { get; init; }

    /// <summary>
    /// Creates a basic metadata instance with minimal information.
    /// </summary>
    /// <param name="fileName">The filename.</param>
    /// <param name="format">The STL format.</param>
    /// <param name="triangleCount">The number of triangles.</param>
    /// <param name="boundingBox">The bounding box.</param>
    /// <returns>A new ModelMetadata instance.</returns>
    public static ModelMetadata Create(
        string fileName,
        STLFormat format,
        int triangleCount,
        BoundingBox boundingBox)
    {
        return new ModelMetadata
        {
            FileName = fileName,
            Format = format,
            TriangleCount = triangleCount,
            BoundingBox = boundingBox
        };
    }

    /// <summary>
    /// Creates a comprehensive metadata instance with detailed information.
    /// </summary>
    /// <param name="fileName">The filename.</param>
    /// <param name="fileSizeBytes">The file size in bytes.</param>
    /// <param name="format">The STL format.</param>
    /// <param name="triangleCount">The number of triangles.</param>
    /// <param name="surfaceArea">The total surface area.</param>
    /// <param name="boundingBox">The bounding box.</param>
    /// <param name="volume">The volume (if calculable).</param>
    /// <param name="lastModified">The last modified date.</param>
    /// <param name="isClosedMesh">Whether the mesh is closed.</param>
    /// <param name="minEdgeLength">The minimum edge length.</param>
    /// <param name="maxEdgeLength">The maximum edge length.</param>
    /// <param name="averageEdgeLength">The average edge length.</param>
    /// <param name="degenerateTriangleCount">The number of degenerate triangles.</param>
    /// <param name="contentHash">The content hash.</param>
    /// <param name="additionalProperties">Additional properties.</param>
    /// <returns>A new comprehensive ModelMetadata instance.</returns>
    public static ModelMetadata CreateDetailed(
        string fileName,
        long fileSizeBytes,
        STLFormat format,
        int triangleCount,
        float surfaceArea,
        BoundingBox boundingBox,
        float? volume = null,
        DateTime? lastModified = null,
        bool? isClosedMesh = null,
        float? minEdgeLength = null,
        float? maxEdgeLength = null,
        float? averageEdgeLength = null,
        int degenerateTriangleCount = 0,
        string? contentHash = null,
        Dictionary<string, string>? additionalProperties = null)
    {
        return new ModelMetadata
        {
            FileName = fileName,
            FileSizeBytes = fileSizeBytes,
            Format = format,
            TriangleCount = triangleCount,
            SurfaceArea = surfaceArea,
            BoundingBox = boundingBox,
            Volume = volume,
            LastModified = lastModified,
            IsClosedMesh = isClosedMesh,
            MinEdgeLength = minEdgeLength,
            MaxEdgeLength = maxEdgeLength,
            AverageEdgeLength = averageEdgeLength,
            DegenerateTriangleCount = degenerateTriangleCount,
            ContentHash = contentHash,
            AdditionalProperties = additionalProperties ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Gets a human-readable file size string.
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            if (FileSizeBytes == 0)
                return "0 bytes";

            string[] units = { "bytes", "KB", "MB", "GB", "TB" };
            int unitIndex = 0;
            double size = FileSizeBytes;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F1} {units[unitIndex]}";
        }
    }

    /// <summary>
    /// Gets the dimensions of the model.
    /// </summary>
    public Vector3 Dimensions => BoundingBox.Size;

    /// <summary>
    /// Gets a summary description of the model.
    /// </summary>
    public string Summary
    {
        get
        {
            var parts = new List<string>
            {
                $"{TriangleCount:N0} triangles",
                $"{Format} format",
                $"Size: {Dimensions.X:F1} × {Dimensions.Y:F1} × {Dimensions.Z:F1}"
            };

            if (Volume.HasValue)
                parts.Add($"Volume: {Volume.Value:F2}");

            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Gets quality metrics for the model.
    /// </summary>
    public ModelQuality Quality
    {
        get
        {
            var score = 100;

            // Deduct points for degenerate triangles
            if (DegenerateTriangleCount > 0)
                score -= System.Math.Min(50, DegenerateTriangleCount * 5);

            // Deduct points if not a closed mesh
            if (IsClosedMesh == false)
                score -= 20;

            // Deduct points for very small or very large triangles
            if (MinEdgeLength.HasValue && MinEdgeLength < 0.001f)
                score -= 10;

            if (MaxEdgeLength.HasValue && AverageEdgeLength.HasValue)
            {
                var ratio = MaxEdgeLength.Value / AverageEdgeLength.Value;
                if (ratio > 100) // Very large variation in triangle sizes
                    score -= 15;
            }

            return score switch
            {
                >= 90 => ModelQuality.Excellent,
                >= 70 => ModelQuality.Good,
                >= 50 => ModelQuality.Fair,
                >= 30 => ModelQuality.Poor,
                _ => ModelQuality.VeryPoor
            };
        }
    }
}

/// <summary>
/// Represents the quality assessment of an STL model.
/// </summary>
public enum ModelQuality
{
    /// <summary>
    /// Very poor quality with significant issues.
    /// </summary>
    VeryPoor,

    /// <summary>
    /// Poor quality with notable issues.
    /// </summary>
    Poor,

    /// <summary>
    /// Fair quality with some minor issues.
    /// </summary>
    Fair,

    /// <summary>
    /// Good quality with minimal issues.
    /// </summary>
    Good,

    /// <summary>
    /// Excellent quality with no significant issues.
    /// </summary>
    Excellent
}
