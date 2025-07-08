using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get available pre-loaded models.
/// </summary>
public class GetAvailablePreloadedModelsQuery : IRequest<Result<IEnumerable<PreloadedModelInfo>>>
{
}

/// <summary>
/// Information about a pre-loaded model.
/// </summary>
public class PreloadedModelInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the model.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the model.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category of the model.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated triangle count.
    /// </summary>
    public int EstimatedTriangleCount { get; set; }

    /// <summary>
    /// Gets or sets the estimated size in bytes.
    /// </summary>
    public long EstimatedSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the model.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Handler for GetAvailablePreloadedModelsQuery.
/// </summary>
public class GetAvailablePreloadedModelsQueryHandler : IRequestHandler<GetAvailablePreloadedModelsQuery, Result<IEnumerable<PreloadedModelInfo>>>
{
    private readonly IEnumerable<IPreloadedModelGenerator> _modelGenerators;

    public GetAvailablePreloadedModelsQueryHandler(IEnumerable<IPreloadedModelGenerator> modelGenerators)
    {
        _modelGenerators = modelGenerators;
    }

    public async Task<Result<IEnumerable<PreloadedModelInfo>>> Handle(GetAvailablePreloadedModelsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var models = _modelGenerators.Select(generator => new PreloadedModelInfo
            {
                Id = generator.ModelId,
                DisplayName = generator.DisplayName,
                Description = generator.Description,
                Category = generator.Category,
                EstimatedTriangleCount = generator.EstimatedTriangleCount,
                EstimatedSizeBytes = generator.EstimatedSizeBytes,
                Tags = generator.Tags.ToList()
            });

            return await Task.FromResult(Result<IEnumerable<PreloadedModelInfo>>.Ok(models));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<PreloadedModelInfo>>.Fail($"Failed to get available pre-loaded models: {ex.Message}");
        }
    }
}
