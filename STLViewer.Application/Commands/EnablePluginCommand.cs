using FluentValidation;
using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to enable a scene plugin.
/// </summary>
public class EnablePluginCommand : IRequest<Result>
{
    /// <summary>
    /// Gets or sets the ID of the plugin to enable.
    /// </summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional configuration to apply when enabling the plugin.
    /// </summary>
    public Dictionary<string, object>? Configuration { get; set; }
}

/// <summary>
/// Validator for EnablePluginCommand.
/// </summary>
public class EnablePluginCommandValidator : AbstractValidator<EnablePluginCommand>
{
    public EnablePluginCommandValidator()
    {
        RuleFor(x => x.PluginId)
            .NotEmpty()
            .WithMessage("Plugin ID is required");
    }
}

/// <summary>
/// Handler for EnablePluginCommand.
/// </summary>
public class EnablePluginCommandHandler : IRequestHandler<EnablePluginCommand, Result>
{
    private readonly IScenePluginManager _pluginManager;

    public EnablePluginCommandHandler(IScenePluginManager pluginManager)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
    }

    public Task<Result> Handle(EnablePluginCommand request, CancellationToken cancellationToken)
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
            return Task.FromResult(Result.Fail($"Error processing enable plugin command: {ex.Message}"));
        }
    }
}
