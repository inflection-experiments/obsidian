using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Application.Commands;

/// <summary>
/// Handler for loading multiple STL files.
/// </summary>
public class LoadFilesCommandHandler : IRequestHandler<LoadFilesCommand, Result<IReadOnlyList<STLModel>>>
{
    private readonly IFileManagementService _fileManagementService;
    private readonly ILogger<LoadFilesCommandHandler> _logger;

    public LoadFilesCommandHandler(
        IFileManagementService fileManagementService,
        ILogger<LoadFilesCommandHandler> logger)
    {
        _fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IReadOnlyList<STLModel>>> Handle(LoadFilesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!request.FilePaths.Any())
            {
                return Result<IReadOnlyList<STLModel>>.Fail("No file paths provided");
            }

            _logger.LogInformation("Loading {Count} STL files", request.FilePaths.Count);

            if (request.ValidateFirst)
            {
                var validationResult = await _fileManagementService.ValidateFilesAsync(request.FilePaths);
                if (!validationResult.IsSuccess)
                {
                    return Result<IReadOnlyList<STLModel>>.Fail($"File validation failed: {validationResult.Error}");
                }

                var validation = validationResult.Value!;
                if (!validation.AllValid && !request.ContinueOnError)
                {
                    var errorMessages = validation.InvalidFiles.Select(e => $"{e.FilePath}: {e.ErrorMessage}");
                    var combinedError = string.Join("; ", errorMessages);
                    return Result<IReadOnlyList<STLModel>>.Fail($"Some files are invalid: {combinedError}");
                }

                // If continuing on error, only load valid files
                if (!validation.AllValid && request.ContinueOnError)
                {
                    var validPaths = validation.ValidFiles.Select(f => f.FilePath);
                    return await _fileManagementService.LoadFilesAsync(validPaths, cancellationToken);
                }
            }

            // Load all files
            var result = await _fileManagementService.LoadFilesAsync(request.FilePaths, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully loaded {Count} STL files", result.Value!.Count);
            }
            else
            {
                _logger.LogError("Failed to load files: {Error}", result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading STL files");
            return Result<IReadOnlyList<STLModel>>.Fail($"Error loading files: {ex.Message}");
        }
    }
}
