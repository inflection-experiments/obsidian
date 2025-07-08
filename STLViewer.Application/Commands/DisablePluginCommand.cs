using FluentValidation;
using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to disable a scene plugin.
/// </summary>
public class DisablePluginCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the ID of the plugin to disable.
    /// </summary>
    public string PluginId { get; set; } = string.Empty;
}

/// <summary>
/// Validator for DisablePluginCommand.
/// </summary>
public class DisablePluginCommandValidator : AbstractValidator<DisablePluginCommand>
{
    public DisablePluginCommandValidator()
    {
        RuleFor(x => x.PluginId)
            .NotEmpty()
            .WithMessage("Plugin ID is required");
    }
}

/// <summary>
/// Handler for DisablePluginCommand.
/// </summary>
public class DisablePluginCommandHandler : IRequestHandler<DisablePluginCommand, Result>
{
    private readonly IScenePluginManager _pluginManager;

    public DisablePluginCommandHandler(IScenePluginManager pluginManager)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
    }

    public Task<Result> Handle(DisablePluginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // This command will be handled by the UI layer which has access to the plugin manager
            // For now, this is a placeholder that indicates the command was processed successfully
            // The actual plugin management logic will be handled at the UI level

            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail($"Error processing disable plugin command: {ex.Message}"));
        }
    }
}
