using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Commands.Scene;

/// <summary>
/// Command to create a new layer in the current scene.
/// </summary>
public record CreateLayerCommand(
    string Name,
    string Description = "",
    STLViewer.Math.Color? Color = null,
    bool IsVisible = true,
    bool IsSelectable = true,
    bool IsLocked = false,
    float Opacity = 1.0f
) : IRequest<Result<Layer>>;

/// <summary>
/// Handler for creating a new layer.
/// </summary>
public class CreateLayerCommandHandler : IRequestHandler<CreateLayerCommand, Result<Layer>>
{
    private readonly ISceneManager _sceneManager;

    /// <summary>
    /// Initializes a new instance of the CreateLayerCommandHandler class.
    /// </summary>
    /// <param name="sceneManager">The scene manager.</param>
    public CreateLayerCommandHandler(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    }

    /// <summary>
    /// Handles the create layer command.
    /// </summary>
    /// <param name="request">The command request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the created layer or an error.</returns>
    public async Task<Result<Layer>> Handle(CreateLayerCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<Layer>.Fail("Layer name cannot be null or empty.");

        try
        {
            var result = _sceneManager.CreateLayer(request.Name, request.Description, request.Color);

            if (result.IsFailure)
                return result;

            var layer = result.Value;

            // Update layer properties if they differ from defaults
            if (!request.IsVisible || !request.IsSelectable || request.IsLocked || request.Opacity != 1.0f)
            {
                var updateResult = _sceneManager.UpdateLayer(
                    layer.Id,
                    isVisible: request.IsVisible,
                    isSelectable: request.IsSelectable,
                    isLocked: request.IsLocked,
                    opacity: request.Opacity);

                if (updateResult.IsFailure)
                    return Result<Layer>.Fail(updateResult.Error);
            }

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            return Result<Layer>.Fail($"Failed to create layer: {ex.Message}");
        }
    }
}
