using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Application.DTOs;
using STLViewer.Domain.Common;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

public class TransformModelCommand : IRequest<Result<STLModelDto>>
{
    public STLModelDto Model { get; set; } = new();
    public Vector3Dto Scale { get; set; } = new() { X = 1.0f, Y = 1.0f, Z = 1.0f };
    public Vector3Dto Rotation { get; set; } = new(); // Rotation in degrees
    public Vector3Dto Translation { get; set; } = new();
    public bool PreserveOriginal { get; set; } = true;
}

public class TransformModelCommandValidator : AbstractValidator<TransformModelCommand>
{
    public TransformModelCommandValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .WithMessage("Model is required")
            .Must(model => model.Triangles.Count > 0)
            .WithMessage("Model must contain at least one triangle");

        RuleFor(x => x.Scale.X)
            .GreaterThan(0)
            .WithMessage("Scale X must be greater than 0");

        RuleFor(x => x.Scale.Y)
            .GreaterThan(0)
            .WithMessage("Scale Y must be greater than 0");

        RuleFor(x => x.Scale.Z)
            .GreaterThan(0)
            .WithMessage("Scale Z must be greater than 0");

        RuleFor(x => x.Rotation.X)
            .InclusiveBetween(-360, 360)
            .WithMessage("Rotation X must be between -360 and 360 degrees");

        RuleFor(x => x.Rotation.Y)
            .InclusiveBetween(-360, 360)
            .WithMessage("Rotation Y must be between -360 and 360 degrees");

        RuleFor(x => x.Rotation.Z)
            .InclusiveBetween(-360, 360)
            .WithMessage("Rotation Z must be between -360 and 360 degrees");
    }
}

public class TransformModelCommandHandler : IRequestHandler<TransformModelCommand, Result<STLModelDto>>
{
    private readonly IMapper _mapper;
    private readonly ILogger<TransformModelCommandHandler> _logger;

    public TransformModelCommandHandler(
        IMapper mapper,
        ILogger<TransformModelCommandHandler> logger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<STLModelDto>> Handle(TransformModelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Transforming STL model with {TriangleCount} triangles",
                request.Model.Triangles.Count);

            var modelToTransform = request.PreserveOriginal ? CloneModel(request.Model) : request.Model;

            // Create transformation matrix
            var transformMatrix = CreateTransformationMatrix(request.Scale, request.Rotation, request.Translation);

            // Transform all triangles
            foreach (var triangle in modelToTransform.Triangles)
            {
                triangle.Vertex1 = TransformVertex(triangle.Vertex1, transformMatrix);
                triangle.Vertex2 = TransformVertex(triangle.Vertex2, transformMatrix);
                triangle.Vertex3 = TransformVertex(triangle.Vertex3, transformMatrix);

                // Recalculate normal after transformation
                triangle.Normal = CalculateNormal(triangle.Vertex1, triangle.Vertex2, triangle.Vertex3);
            }

            // Update model metadata
            if (modelToTransform.Metadata != null)
            {
                modelToTransform.Metadata.Version = IncrementVersion(modelToTransform.Metadata.Version);
                modelToTransform.Metadata.CustomProperties["LastTransformed"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            }

            modelToTransform.ModifiedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully transformed STL model");

            await Task.CompletedTask; // Placeholder for async operations

            return Result<STLModelDto>.Ok(modelToTransform);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transforming STL model");
            return Result<STLModelDto>.Fail($"Error transforming model: {ex.Message}");
        }
    }

        private Matrix4x4 CreateTransformationMatrix(Vector3Dto scale, Vector3Dto rotation, Vector3Dto translation)
    {
        // Create individual transformation matrices
        var scaleMatrix = Matrix4x4.CreateScale(new Vector3(scale.X, scale.Y, scale.Z));

        var rotationMatrix = Matrix4x4.Multiply(
            Matrix4x4.Multiply(
                Matrix4x4.CreateRotationX(DegreesToRadians(rotation.X)),
                Matrix4x4.CreateRotationY(DegreesToRadians(rotation.Y))),
            Matrix4x4.CreateRotationZ(DegreesToRadians(rotation.Z)));

        var translationMatrix = Matrix4x4.CreateTranslation(new Vector3(translation.X, translation.Y, translation.Z));

        // Combine transformations: Scale -> Rotate -> Translate
        return Matrix4x4.Multiply(Matrix4x4.Multiply(scaleMatrix, rotationMatrix), translationMatrix);
    }

        private Vector3Dto TransformVertex(Vector3Dto vertex, Matrix4x4 transformMatrix)
    {
        var vector = new Vector3(vertex.X, vertex.Y, vertex.Z);
        var transformedVector = transformMatrix.TransformPoint(vector);

        return new Vector3Dto
        {
            X = transformedVector.X,
            Y = transformedVector.Y,
            Z = transformedVector.Z
        };
    }

    private Vector3Dto CalculateNormal(Vector3Dto v1, Vector3Dto v2, Vector3Dto v3)
    {
        var vec1 = new Vector3(v2.X - v1.X, v2.Y - v1.Y, v2.Z - v1.Z);
        var vec2 = new Vector3(v3.X - v1.X, v3.Y - v1.Y, v3.Z - v1.Z);
        var normal = Vector3.Cross(vec1, vec2);
        normal = normal.Normalized();

        return new Vector3Dto
        {
            X = normal.X,
            Y = normal.Y,
            Z = normal.Z
        };
    }

    private STLModelDto CloneModel(STLModelDto original)
    {
        return new STLModelDto
        {
            Name = original.Name,
            FilePath = original.FilePath,
            Format = original.Format,
            Metadata = original.Metadata != null ? new ModelMetadataDto
            {
                Title = original.Metadata.Title,
                Description = original.Metadata.Description,
                Author = original.Metadata.Author,
                Version = original.Metadata.Version,
                CustomProperties = new Dictionary<string, string>(original.Metadata.CustomProperties)
            } : null,
            Triangles = original.Triangles.Select(t => new TriangleDto
            {
                Vertex1 = new Vector3Dto { X = t.Vertex1.X, Y = t.Vertex1.Y, Z = t.Vertex1.Z },
                Vertex2 = new Vector3Dto { X = t.Vertex2.X, Y = t.Vertex2.Y, Z = t.Vertex2.Z },
                Vertex3 = new Vector3Dto { X = t.Vertex3.X, Y = t.Vertex3.Y, Z = t.Vertex3.Z },
                Normal = new Vector3Dto { X = t.Normal.X, Y = t.Normal.Y, Z = t.Normal.Z }
            }).ToList(),
            CreatedAt = original.CreatedAt,
            ModifiedAt = DateTime.UtcNow,
            FileSize = original.FileSize,
            IsValid = original.IsValid,
            ValidationErrors = new List<string>(original.ValidationErrors)
        };
    }

    private string IncrementVersion(string? currentVersion)
    {
        if (string.IsNullOrEmpty(currentVersion))
            return "1.0";

        if (Version.TryParse(currentVersion, out var version))
        {
            return new Version(version.Major, version.Minor + 1).ToString();
        }

        return currentVersion + ".1";
    }

    private float DegreesToRadians(float degrees) => degrees * (float)System.Math.PI / 180.0f;
}
