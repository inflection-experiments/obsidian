using MediatR;
using STLViewer.Core.Interfaces;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get available scene plugins.
/// </summary>
public class GetAvailablePluginsQuery : IRequest<List<PluginInfoDto>>
{
    /// <summary>
    /// Gets or sets whether to include only enabled plugins.
    /// </summary>
    public bool EnabledOnly { get; set; } = false;
}

/// <summary>
/// DTO for plugin information.
/// </summary>
public class PluginInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsConfigurable { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Handler for GetAvailablePluginsQuery.
/// </summary>
public class GetAvailablePluginsQueryHandler : IRequestHandler<GetAvailablePluginsQuery, List<PluginInfoDto>>
{
    private readonly IScenePluginManager _pluginManager;

    public GetAvailablePluginsQueryHandler(IScenePluginManager pluginManager)
    {
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
    }

    public Task<List<PluginInfoDto>> Handle(GetAvailablePluginsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // This query will be handled by the UI layer which has access to the plugin manager
            // For now, return an empty collection as a placeholder
            var emptyResult = new List<PluginInfoDto>();
            return Task.FromResult(emptyResult);
        }
        catch (Exception)
        {
            // In case of error, return empty list rather than throwing
            return Task.FromResult(new List<PluginInfoDto>());
        }
    }
}
