using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Application.DTOs;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.Enums;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

public class SaveSTLCommand : IRequest<Result<string>>
{
    public STLModelDto Model { get; set; } = new();
    public string FilePath { get; set; } = string.Empty;
    public STLFormat Format { get; set; } = STLFormat.Binary;
    public bool Overwrite { get; set; } = false;
    public bool ValidateBeforeSaving { get; set; } = true;
}

public class SaveSTLCommandValidator : AbstractValidator<SaveSTLCommand>
{
    public SaveSTLCommandValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty()
            .WithMessage("File path is required")
            .Must(path => Path.GetExtension(path).ToLowerInvariant() == ".stl")
            .WithMessage("File must have .stl extension");

        RuleFor(x => x.Model)
            .NotNull()
            .WithMessage("Model is required")
            .Must(model => model.Triangles.Count > 0)
            .WithMessage("Model must contain at least one triangle");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid STL format specified");

        RuleFor(x => x.FilePath)
            .Must((command, path) => command.Overwrite || !File.Exists(path))
            .WithMessage("File already exists and overwrite is not enabled");
    }
}

public class SaveSTLCommandHandler : IRequestHandler<SaveSTLCommand, Result<string>>
{
    private readonly ISTLParser _stlParser;
    private readonly IMapper _mapper;
    private readonly ILogger<SaveSTLCommandHandler> _logger;

    public SaveSTLCommandHandler(
        ISTLParser stlParser,
        IMapper mapper,
        ILogger<SaveSTLCommandHandler> logger)
    {
        _stlParser = stlParser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<string>> Handle(SaveSTLCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Saving STL file: {FilePath} in {Format} format",
                request.FilePath, request.Format);

            // Convert DTO to domain model using factory method
            var metadata = _mapper.Map<ModelMetadata>(request.Model.Metadata) with
            {
                Format = request.Format
            };

            var triangles = _mapper.Map<List<Triangle>>(request.Model.Triangles);
            var rawData = new byte[0]; // Placeholder - would need actual raw data

            var modelResult = STLModel.Create(metadata, triangles, rawData);
            if (modelResult.IsFailure)
                return Result<string>.Fail(modelResult.Error);

            var stlModel = modelResult.Value;

            // Validate model if requested
            if (request.ValidateBeforeSaving)
            {
                var validationResult = await ValidateModel(stlModel, cancellationToken);
                                if (validationResult.IsFailure)
                {
                    _logger.LogError("Model validation failed before saving: {Error}",
                        validationResult.Error);
                    return Result<string>.Fail(validationResult.Error);
                }
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(request.FilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save the model
            var saveResult = await _stlParser.SaveAsync(stlModel, request.FilePath, cancellationToken);

                        if (saveResult.IsFailure)
            {
                _logger.LogError("Failed to save STL file: {FilePath}. Error: {Error}",
                    request.FilePath, saveResult.Error);
                return Result<string>.Fail(saveResult.Error);
            }

            _logger.LogInformation("Successfully saved STL file: {FilePath} with {TriangleCount} triangles",
                request.FilePath, stlModel.Triangles.Count);

            return Result.Ok(request.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving STL file: {FilePath}", request.FilePath);
            return Result<string>.Fail($"Error saving STL file: {ex.Message}");
        }
    }

    private async Task<Result> ValidateModel(STLModel model, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        // Basic validation
        if (model.Triangles.Count == 0)
            errors.Add("Model contains no triangles");

        // Check for degenerate triangles
        var degenerateTriangleCount = model.Triangles.Count(t => !t.IsValid);
        if (degenerateTriangleCount > 0)
            errors.Add($"Model contains {degenerateTriangleCount} invalid triangles");

        // Check for invalid vertices
        var invalidVertexCount = model.Triangles.Count(t =>
            HasInvalidVertex(t.Vertex1) || HasInvalidVertex(t.Vertex2) || HasInvalidVertex(t.Vertex3));
        if (invalidVertexCount > 0)
            errors.Add($"Model contains {invalidVertexCount} triangles with invalid vertices");

        await Task.CompletedTask; // Placeholder for async validation if needed

        return errors.Any() ? Result.Fail(string.Join("; ", errors)) : Result.Ok();
    }

    private bool HasInvalidVertex(Vector3 vertex)
    {
        return float.IsNaN(vertex.X) || float.IsNaN(vertex.Y) || float.IsNaN(vertex.Z) ||
               float.IsInfinity(vertex.X) || float.IsInfinity(vertex.Y) || float.IsInfinity(vertex.Z);
    }
}
