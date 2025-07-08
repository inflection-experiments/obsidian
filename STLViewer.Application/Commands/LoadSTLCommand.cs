using AutoMapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Application.DTOs;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Commands;

public class LoadSTLCommand : IRequest<Result<STLModelDto>>
{
    public string FilePath { get; set; } = string.Empty;
    public bool ValidateModel { get; set; } = true;
    public bool LoadTriangles { get; set; } = true;
}

public class LoadSTLCommandValidator : AbstractValidator<LoadSTLCommand>
{
    public LoadSTLCommandValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty()
            .WithMessage("File path is required")
            .Must(path => File.Exists(path))
            .WithMessage("File does not exist")
            .Must(path => Path.GetExtension(path).ToLowerInvariant() == ".stl")
            .WithMessage("File must be an STL file");
    }
}

public class LoadSTLCommandHandler : IRequestHandler<LoadSTLCommand, Result<STLModelDto>>
{
    private readonly ISTLParser _stlParser;
    private readonly IMapper _mapper;
    private readonly ILogger<LoadSTLCommandHandler> _logger;

    public LoadSTLCommandHandler(
        ISTLParser stlParser,
        IMapper mapper,
        ILogger<LoadSTLCommandHandler> logger)
    {
        _stlParser = stlParser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<STLModelDto>> Handle(LoadSTLCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Loading STL file: {FilePath}", request.FilePath);

            var parseResult = await _stlParser.ParseAsync(request.FilePath, cancellationToken);

                        if (parseResult.IsFailure)
            {
                _logger.LogError("Failed to parse STL file: {FilePath}. Error: {Error}",
                    request.FilePath, parseResult.Error);
                return Result<STLModelDto>.Fail(parseResult.Error);
            }

            var stlModel = parseResult.Value;
            var modelDto = _mapper.Map<STLModelDto>(stlModel);

            // Add file information
            var fileInfo = new FileInfo(request.FilePath);
            modelDto.FilePath = request.FilePath;
            modelDto.FileSize = fileInfo.Length;
            modelDto.CreatedAt = fileInfo.CreationTime;
            modelDto.ModifiedAt = fileInfo.LastWriteTime;

            // Validate if requested
            if (request.ValidateModel)
            {
                var validationResult = await ValidateModel(stlModel, cancellationToken);
                modelDto.IsValid = validationResult.IsSuccess;
                modelDto.ValidationErrors = validationResult.IsFailure ? new List<string> { validationResult.Error } : new List<string>();
            }

            _logger.LogInformation("Successfully loaded STL file: {FilePath} with {TriangleCount} triangles",
                request.FilePath, stlModel.Triangles.Count);

            return Result.Ok(modelDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading STL file: {FilePath}", request.FilePath);
            return Result<STLModelDto>.Fail($"Error loading STL file: {ex.Message}");
        }
    }

    private async Task<Result> ValidateModel(STLModel model, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        // Basic validation
        if (model.Triangles.Count == 0)
            errors.Add("Model contains no triangles");

        // Check for degenerate triangles
        var degenerateCount = model.Triangles.Count(t => IsDegenerateTriangle(t));
        if (degenerateCount > 0)
            errors.Add($"Model contains {degenerateCount} degenerate triangles");

        // Check for valid normals
        var invalidNormalCount = model.Triangles.Count(t => !IsValidNormal(t.Normal));
        if (invalidNormalCount > 0)
            errors.Add($"Model contains {invalidNormalCount} triangles with invalid normals");

        await Task.CompletedTask; // Placeholder for async validation if needed

        return errors.Any() ? Result.Fail(string.Join("; ", errors)) : Result.Ok();
    }

    private bool IsDegenerateTriangle(Triangle triangle)
    {
        // Check if triangle has zero area (vertices are collinear)
        var v1 = triangle.Vertex2 - triangle.Vertex1;
        var v2 = triangle.Vertex3 - triangle.Vertex1;
        var cross = Vector3.Cross(v1, v2);
        return cross.Length < 1e-6f;
    }

    private bool IsValidNormal(Vector3 normal)
    {
        return !float.IsNaN(normal.X) && !float.IsNaN(normal.Y) && !float.IsNaN(normal.Z) &&
               !float.IsInfinity(normal.X) && !float.IsInfinity(normal.Y) && !float.IsInfinity(normal.Z);
    }
}
