using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Core.Interfaces;

namespace STLViewer.Application.Commands;

/// <summary>
/// Handler for clearing recent files.
/// </summary>
public class ClearRecentFilesCommandHandler : IRequestHandler<ClearRecentFilesCommand>
{
    private readonly IFileManagementService _fileManagementService;
    private readonly ILogger<ClearRecentFilesCommandHandler> _logger;

    public ClearRecentFilesCommandHandler(
        IFileManagementService fileManagementService,
        ILogger<ClearRecentFilesCommandHandler> logger)
    {
        _fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Handle(ClearRecentFilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _fileManagementService.ClearRecentFiles();
            _logger.LogInformation("Recent files list cleared");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing recent files");
            throw;
        }
    }
}
