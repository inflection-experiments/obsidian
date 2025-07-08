using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Queries.Scene;

/// <summary>
/// Query to get statistics for the current scene.
/// </summary>
public record GetSceneStatisticsQuery() : IRequest<Result<SceneStatistics>>;

/// <summary>
/// Handler for getting scene statistics.
/// </summary>
public class GetSceneStatisticsQueryHandler : IRequestHandler<GetSceneStatisticsQuery, Result<SceneStatistics>>
{
    private readonly ISceneManager _sceneManager;

    /// <summary>
    /// Initializes a new instance of the GetSceneStatisticsQueryHandler class.
    /// </summary>
    /// <param name="sceneManager">The scene manager.</param>
    public GetSceneStatisticsQueryHandler(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    }

    /// <summary>
    /// Handles the get scene statistics query.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the scene statistics or an error.</returns>
    public async Task<Result<SceneStatistics>> Handle(GetSceneStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = _sceneManager.GetSceneStatistics();
            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Result<SceneStatistics>.Fail($"Failed to get scene statistics: {ex.Message}");
        }
    }
}
