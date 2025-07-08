using AutoMapper;
using FluentValidation;
using MediatR;
using STLViewer.Application.DTOs;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to load a pre-loaded STL model.
/// </summary>
public class LoadPreloadedModelCommand : IRequest<Result<STLModelDto>>
{
    /// <summary>
    /// Gets or sets the name of the pre-loaded model to load.
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to include triangle data in the result.
    /// </summary>
    public bool IncludeTriangles { get; set; } = true;
}

/// <summary>
/// Validator for LoadPreloadedModelCommand.
/// </summary>
public class LoadPreloadedModelCommandValidator : AbstractValidator<LoadPreloadedModelCommand>
{
    public LoadPreloadedModelCommandValidator()
    {
        RuleFor(x => x.ModelName)
            .NotEmpty()
            .WithMessage("Model name is required");

        RuleFor(x => x.ModelName)
            .Must(BeValidModelName)
            .WithMessage("Invalid model name. Valid models: fighter-plane");
    }

    private bool BeValidModelName(string modelName)
    {
        var validModels = new[] { "fighter-plane" };
        return validModels.Contains(modelName.ToLowerInvariant());
    }
}

/// <summary>
/// Handler for LoadPreloadedModelCommand.
/// </summary>
public class LoadPreloadedModelCommandHandler : IRequestHandler<LoadPreloadedModelCommand, Result<STLModelDto>>
{
    private readonly IEnumerable<IPreloadedModelGenerator> _modelGenerators;
    private readonly IMapper _mapper;

    public LoadPreloadedModelCommandHandler(
        IEnumerable<IPreloadedModelGenerator> modelGenerators,
        IMapper mapper)
    {
        _modelGenerators = modelGenerators;
        _mapper = mapper;
    }

    public async Task<Result<STLModelDto>> Handle(LoadPreloadedModelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var generator = _modelGenerators.FirstOrDefault(g =>
                g.ModelId.Equals(request.ModelName, StringComparison.OrdinalIgnoreCase));

            if (generator == null)
            {
                return Result<STLModelDto>.Fail($"Unknown model: {request.ModelName}");
            }

            var modelResult = await Task.FromResult(generator.GenerateModel());

            if (modelResult.IsFailure)
                return Result<STLModelDto>.Fail(modelResult.Error);

            var model = modelResult.Value;
            var dto = _mapper.Map<STLModelDto>(model);

            // Optionally exclude triangles to reduce payload size
            if (!request.IncludeTriangles)
            {
                dto.Triangles = new List<STLViewer.Application.DTOs.TriangleDto>();
            }

            return Result<STLModelDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<STLModelDto>.Fail($"Failed to load pre-loaded model '{request.ModelName}': {ex.Message}");
        }
    }
}
