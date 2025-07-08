using AutoMapper;
using STLViewer.Application.DTOs;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<STLModel, STLModelDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Metadata.FileName))
            .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Metadata.Format))
            .ForMember(dest => dest.Triangles, opt => opt.MapFrom(src => src.Triangles))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => src.Metadata))
            .ForMember(dest => dest.FilePath, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.FileSize, opt => opt.Ignore())
            .ForMember(dest => dest.IsValid, opt => opt.Ignore())
            .ForMember(dest => dest.ValidationErrors, opt => opt.Ignore());

        // Note: Creating STLModel from DTO requires using the static factory methods
        // This mapping is simplified and may need additional logic
        CreateMap<STLModelDto, STLModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Metadata, opt => opt.Ignore())
            .ForMember(dest => dest.RawData, opt => opt.Ignore())
            .ForMember(dest => dest.Triangles, opt => opt.Ignore())
            .ForMember(dest => dest.BoundingBox, opt => opt.Ignore())
            .ForMember(dest => dest.TriangleCount, opt => opt.Ignore())
            .ForMember(dest => dest.SurfaceArea, opt => opt.Ignore())
            .ForMember(dest => dest.IsValid, opt => opt.Ignore())
            .ForMember(dest => dest.Center, opt => opt.Ignore())
            .ForMember(dest => dest.Dimensions, opt => opt.Ignore());

        CreateMap<Triangle, TriangleDto>()
            .ForMember(dest => dest.Vertex1, opt => opt.MapFrom(src => src.Vertex1))
            .ForMember(dest => dest.Vertex2, opt => opt.MapFrom(src => src.Vertex2))
            .ForMember(dest => dest.Vertex3, opt => opt.MapFrom(src => src.Vertex3))
            .ForMember(dest => dest.Normal, opt => opt.MapFrom(src => src.Normal));

        CreateMap<TriangleDto, Triangle>()
            .ForMember(dest => dest.Vertex1, opt => opt.MapFrom(src => src.Vertex1))
            .ForMember(dest => dest.Vertex2, opt => opt.MapFrom(src => src.Vertex2))
            .ForMember(dest => dest.Vertex3, opt => opt.MapFrom(src => src.Vertex3))
            .ForMember(dest => dest.Normal, opt => opt.MapFrom(src => src.Normal));

        CreateMap<Vector3, Vector3Dto>()
            .ForMember(dest => dest.X, opt => opt.MapFrom(src => src.X))
            .ForMember(dest => dest.Y, opt => opt.MapFrom(src => src.Y))
            .ForMember(dest => dest.Z, opt => opt.MapFrom(src => src.Z));

        CreateMap<Vector3Dto, Vector3>()
            .ForMember(dest => dest.X, opt => opt.MapFrom(src => src.X))
            .ForMember(dest => dest.Y, opt => opt.MapFrom(src => src.Y))
            .ForMember(dest => dest.Z, opt => opt.MapFrom(src => src.Z));

        CreateMap<ModelMetadata, ModelMetadataDto>()
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.FileName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.AdditionalProperties.GetValueOrDefault("Description", "")))
            .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.AdditionalProperties.GetValueOrDefault("Author", "")))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.AdditionalProperties.GetValueOrDefault("Version", "")))
            .ForMember(dest => dest.CustomProperties, opt => opt.MapFrom(src => src.AdditionalProperties));

        CreateMap<ModelMetadataDto, ModelMetadata>()
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.AdditionalProperties, opt => opt.MapFrom(src => CreateAdditionalProperties(src)))
            .ForMember(dest => dest.Format, opt => opt.Ignore())
            .ForMember(dest => dest.FileSizeBytes, opt => opt.Ignore())
            .ForMember(dest => dest.TriangleCount, opt => opt.Ignore())
            .ForMember(dest => dest.SurfaceArea, opt => opt.Ignore())
            .ForMember(dest => dest.Volume, opt => opt.Ignore())
            .ForMember(dest => dest.BoundingBox, opt => opt.Ignore())
            .ForMember(dest => dest.LastModified, opt => opt.Ignore())
            .ForMember(dest => dest.LoadedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsClosedMesh, opt => opt.Ignore())
            .ForMember(dest => dest.MinEdgeLength, opt => opt.Ignore())
            .ForMember(dest => dest.MaxEdgeLength, opt => opt.Ignore())
            .ForMember(dest => dest.AverageEdgeLength, opt => opt.Ignore())
            .ForMember(dest => dest.DegenerateTriangleCount, opt => opt.Ignore())
            .ForMember(dest => dest.ContentHash, opt => opt.Ignore());

        CreateMap<BoundingBox, BoundingBoxDto>()
            .ForMember(dest => dest.Min, opt => opt.MapFrom(src => src.Min))
            .ForMember(dest => dest.Max, opt => opt.MapFrom(src => src.Max))
            .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.Size))
            .ForMember(dest => dest.Center, opt => opt.MapFrom(src => src.Center));

        CreateMap<BoundingBoxDto, BoundingBox>()
            .ConvertUsing(src => new BoundingBox(
                new Vector3(src.Min.X, src.Min.Y, src.Min.Z),
                new Vector3(src.Max.X, src.Max.Y, src.Max.Z)));
    }

    private Dictionary<string, string> CreateAdditionalProperties(ModelMetadataDto dto)
    {
        var properties = new Dictionary<string, string>(dto.CustomProperties);
        if (!string.IsNullOrEmpty(dto.Description))
            properties["Description"] = dto.Description;
        if (!string.IsNullOrEmpty(dto.Author))
            properties["Author"] = dto.Author;
        if (!string.IsNullOrEmpty(dto.Version))
            properties["Version"] = dto.Version;
        return properties;
    }
}
