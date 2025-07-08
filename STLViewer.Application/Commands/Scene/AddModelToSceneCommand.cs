using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Application.Commands.Scene;

/// <summary>
/// Command to add an STL model to a scene.
/// </summary>
public record AddModelToSceneCommand(
    STLModel Model,
    string? ObjectName = null,
    Guid? LayerId = null,
    Guid? ParentId = null
) : IRequest<Result<SceneObject>>;

/// <summary>
/// Handler for adding an STL model to a scene.
/// </summary>
public class AddModelToSceneCommandHandler : IRequestHandler<AddModelToSceneCommand, Result<SceneObject>>
{
    private readonly ISceneManager _sceneManager;

    /// <summary>
    /// Initializes a new instance of the AddModelToSceneCommandHandler class.
    /// </summary>
    /// <param name="sceneManager">The scene manager.</param>
    public AddModelToSceneCommandHandler(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    }

    /// <summary>
    /// Handles the add model to scene command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created scene object or an error.</returns>
    public async Task<Result<SceneObject>> Handle(AddModelToSceneCommand request, CancellationToken cancellationToken)
    {
        if (request.Model == null)
            return Result<SceneObject>.Fail("Model cannot be null.");

        try
        {
            SceneNode? parent = null;
            if (request.ParentId.HasValue)
            {
                var currentScene = _sceneManager.CurrentScene;
                if (currentScene == null)
                    return Result<SceneObject>.Fail("No active scene.");

                parent = currentScene.FindNode(request.ParentId.Value);
                if (parent == null)
                    return Result<SceneObject>.Fail($"Parent node with ID {request.ParentId.Value} not found.");
            }

            var result = _sceneManager.AddModelToScene(
                request.Model,
                request.ObjectName,
                request.LayerId,
                parent);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Result<SceneObject>.Fail($"Failed to add model to scene: {ex.Message}");
        }
    }
}
