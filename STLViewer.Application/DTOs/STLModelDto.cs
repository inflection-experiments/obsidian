using STLViewer.Domain.Enums;

namespace STLViewer.Application.DTOs;

public class STLModelDto
{
    public string? Name { get; set; }
    public string? FilePath { get; set; }
    public STLFormat Format { get; set; }
    public ModelMetadataDto? Metadata { get; set; }
    public List<TriangleDto> Triangles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public long FileSize { get; set; }
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
}

public class ModelMetadataDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? Version { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}

public class TriangleDto
{
    public Vector3Dto Vertex1 { get; set; } = new();
    public Vector3Dto Vertex2 { get; set; } = new();
    public Vector3Dto Vertex3 { get; set; } = new();
    public Vector3Dto Normal { get; set; } = new();
}

public class Vector3Dto
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public class BoundingBoxDto
{
    public Vector3Dto Min { get; set; } = new();
    public Vector3Dto Max { get; set; } = new();
    public Vector3Dto Size { get; set; } = new();
    public Vector3Dto Center { get; set; } = new();
}
