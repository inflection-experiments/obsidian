using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Application.Commands.Scene;

/// <summary>
/// Command to create a new scene.
/// </summary>
public record CreateSceneCommand(string Name, string Description = "") : IRequest<Result<Domain.Entities.Scene>>;

/// <summary>
/// Handler for creating a new scene.
/// </summary>
public class CreateSceneCommandHandler : IRequestHandler<CreateSceneCommand, Result<Domain.Entities.Scene>>
{
    private readonly ISceneManager _sceneManager;
    private readonly ISceneRepository _sceneRepository;

    /// <summary>
    /// Initializes a new instance of the CreateSceneCommandHandler class.
    /// </summary>
    /// <param name="sceneManager">The scene manager.</param>
    /// <param name="sceneRepository">The scene repository.</param>
    public CreateSceneCommandHandler(ISceneManager sceneManager, ISceneRepository sceneRepository)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _sceneRepository = sceneRepository ?? throw new ArgumentNullException(nameof(sceneRepository));
    }

    /// <summary>
    /// Handles the create scene command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created scene or an error.</returns>
    public async Task<Result<Domain.Entities.Scene>> Handle(CreateSceneCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Domain.Entities.Scene>.Fail("Scene name cannot be null or empty.");

        try
        {
            var createResult = _sceneManager.CreateScene(request.Name);
            if (createResult.IsFailure)
                return createResult;

            var scene = createResult.Value;

            // Set description if provided
            if (!string.IsNullOrWhiteSpace(request.Description))
                scene.Description = request.Description;

            // Save the scene
            var saveResult = await _sceneRepository.SaveAsync(scene, cancellationToken);
            if (saveResult.IsFailure)
                return Result<Domain.Entities.Scene>.Fail(saveResult.Error);

            return Result<Domain.Entities.Scene>.Ok(scene);
        }
        catch (Exception ex)
        {
            return Result<Domain.Entities.Scene>.Fail($"Failed to create scene: {ex.Message}");
        }
    }
}
