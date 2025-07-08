using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Queries;

/// <summary>
/// Handler for getting recent files.
/// </summary>
public class GetRecentFilesQueryHandler : IRequestHandler<GetRecentFilesQuery, IReadOnlyList<Domain.ValueObjects.FileInfo>>
{
    private readonly IFileManagementService _fileManagementService;
    private readonly ILogger<GetRecentFilesQueryHandler> _logger;

    public GetRecentFilesQueryHandler(
        IFileManagementService fileManagementService,
        ILogger<GetRecentFilesQueryHandler> logger)
    {
        _fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<Domain.ValueObjects.FileInfo>> Handle(GetRecentFilesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.RefreshStatus)
            {
                await _fileManagementService.RefreshRecentFilesAsync();
            }

            var recentFiles = _fileManagementService.RecentFiles;
            _logger.LogDebug("Retrieved {Count} recent files", recentFiles.Count);

            return recentFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent files");
            return Array.Empty<Domain.ValueObjects.FileInfo>();
        }
    }
}
