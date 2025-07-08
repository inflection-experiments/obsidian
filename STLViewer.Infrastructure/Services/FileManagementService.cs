using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Infrastructure.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;

namespace STLViewer.Infrastructure.Services;

/// <summary>
/// Implementation of file management service for STL files.
/// </summary>
public class FileManagementService : IFileManagementService, IDisposable
{
    private readonly ISTLParser _stlParser;
    private readonly ILogger<FileManagementService> _logger;
    private readonly ApplicationOptions _options;
    private readonly List<Domain.ValueObjects.FileInfo> _recentFiles;
    private readonly object _recentFilesLock = new();
    private readonly string _recentFilesPath;
    private readonly ConcurrentDictionary<string, FileOperationContext> _activeOperations;
    private bool _disposed;

    private const int DefaultMaxRecentFiles = 10;
    private const long MaxFileSizeBytes = 500 * 1024 * 1024; // 500MB
    private static readonly string[] ValidExtensions = { ".stl" };

    public FileManagementService(
        ISTLParser stlParser,
        ILogger<FileManagementService> logger,
        IOptions<ApplicationOptions> options)
    {
        _stlParser = stlParser ?? throw new ArgumentNullException(nameof(stlParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _recentFiles = new List<Domain.ValueObjects.FileInfo>();
        _activeOperations = new ConcurrentDictionary<string, FileOperationContext>();

        // Set up recent files storage path
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "STLViewer");
        Directory.CreateDirectory(appFolder);
        _recentFilesPath = Path.Combine(appFolder, "recent_files.json");

        MaxRecentFiles = DefaultMaxRecentFiles;

        // Load recent files on startup
        _ = LoadRecentFilesAsync();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Domain.ValueObjects.FileInfo> RecentFiles
    {
        get
        {
            lock (_recentFilesLock)
            {
                return _recentFiles.ToList();
            }
        }
    }

    /// <inheritdoc/>
    public int MaxRecentFiles { get; set; }

    /// <inheritdoc/>
    public event EventHandler<IReadOnlyList<Domain.ValueObjects.FileInfo>>? RecentFilesChanged;

    /// <inheritdoc/>
    public event EventHandler<FileOperationProgressEventArgs>? FileOperationStarted;

    /// <inheritdoc/>
    public event EventHandler<FileOperationProgressEventArgs>? FileOperationProgress;

    /// <inheritdoc/>
    public event EventHandler<FileOperationProgressEventArgs>? FileOperationCompleted;

    /// <inheritdoc/>
    public async Task<Result<FileValidationResult>> ValidateFilesAsync(IEnumerable<string> filePaths)
    {
        try
        {
            var validFiles = new List<Domain.ValueObjects.FileInfo>();
            var invalidFiles = new List<FileValidationError>();

            foreach (var filePath in filePaths)
            {
                var validation = await ValidateSingleFileAsync(filePath);
                if (validation.IsSuccess)
                {
                    validFiles.Add(validation.Value!);
                }
                else
                {
                    var errorType = DetermineErrorType(validation.Error!);
                    invalidFiles.Add(new FileValidationError(filePath, validation.Error!, errorType));
                }
            }

            var result = new FileValidationResult(validFiles, invalidFiles);
            return Result<FileValidationResult>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating files");
            return Result<FileValidationResult>.Fail($"Error validating files: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<STLModel>> LoadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var context = new FileOperationContext(operationId, new[] { filePath });
        _activeOperations.TryAdd(operationId, context);

        try
        {
            // Validate file first
            var validation = await ValidateSingleFileAsync(filePath);
            if (!validation.IsSuccess)
            {
                return Result<STLModel>.Fail(validation.Error!);
            }

            var fileInfo = validation.Value!;

            // Report start
            var startArgs = new FileOperationProgressEventArgs(
                operationId, filePath, 1, 0, FileOperationStatus.Starting, startTime: DateTime.Now);
            FileOperationStarted?.Invoke(this, startArgs);

            // Load the file
            var loadResult = await _stlParser.ParseAsync(filePath, cancellationToken);

            if (loadResult.IsSuccess)
            {
                // Add to recent files
                AddToRecentFiles(fileInfo);

                // Report completion
                var completeArgs = new FileOperationProgressEventArgs(
                    operationId, filePath, 1, 1, FileOperationStatus.Completed, startTime: context.StartTime);
                FileOperationCompleted?.Invoke(this, completeArgs);

                _logger.LogInformation("Successfully loaded STL file: {FilePath}", filePath);
                return loadResult;
            }
            else
            {
                // Report failure
                var failArgs = new FileOperationProgressEventArgs(
                    operationId, filePath, 1, 0, FileOperationStatus.Failed, loadResult.Error, context.StartTime);
                FileOperationCompleted?.Invoke(this, failArgs);

                return loadResult;
            }
        }
        finally
        {
            _activeOperations.TryRemove(operationId, out _);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<STLModel>>> LoadFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        var operationId = Guid.NewGuid().ToString();
        var filePathList = filePaths.ToList();
        var context = new FileOperationContext(operationId, filePathList);
        _activeOperations.TryAdd(operationId, context);

        try
        {
            // Validate all files first
            var validationResult = await ValidateFilesAsync(filePathList);
            if (!validationResult.IsSuccess)
            {
                return Result<IReadOnlyList<STLModel>>.Fail(validationResult.Error!);
            }

            var validation = validationResult.Value!;
            if (!validation.AllValid)
            {
                var errorMessages = validation.InvalidFiles.Select(e => $"{e.FilePath}: {e.ErrorMessage}");
                var combinedError = string.Join("; ", errorMessages);
                return Result<IReadOnlyList<STLModel>>.Fail($"Some files are invalid: {combinedError}");
            }

            var totalFiles = validation.ValidFiles.Count;
            var loadedModels = new List<STLModel>();

            // Report start
            var startArgs = new FileOperationProgressEventArgs(
                operationId, null, totalFiles, 0, FileOperationStatus.Starting, startTime: DateTime.Now);
            FileOperationStarted?.Invoke(this, startArgs);

            // Load files with progress reporting
            for (int i = 0; i < validation.ValidFiles.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    var cancelArgs = new FileOperationProgressEventArgs(
                        operationId, null, totalFiles, i, FileOperationStatus.Cancelled, startTime: context.StartTime);
                    FileOperationCompleted?.Invoke(this, cancelArgs);
                    return Result<IReadOnlyList<STLModel>>.Fail("Operation was cancelled");
                }

                var fileInfo = validation.ValidFiles[i];

                // Report progress
                var progressArgs = new FileOperationProgressEventArgs(
                    operationId, fileInfo.FileName, totalFiles, i, FileOperationStatus.InProgress,
                    startTime: context.StartTime, estimatedTimeRemaining: EstimateTimeRemaining(context, i, totalFiles));
                FileOperationProgress?.Invoke(this, progressArgs);

                try
                {
                    var loadResult = await _stlParser.ParseAsync(fileInfo.FilePath, cancellationToken);
                    if (loadResult.IsSuccess)
                    {
                        loadedModels.Add(loadResult.Value!);
                        AddToRecentFiles(fileInfo);
                        _logger.LogDebug("Loaded file {Index}/{Total}: {FilePath}", i + 1, totalFiles, fileInfo.FilePath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to load file {FilePath}: {Error}", fileInfo.FilePath, loadResult.Error);
                        // For batch loading, we continue with other files but log the error
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading file {FilePath}", fileInfo.FilePath);
                    // Continue with other files
                }
            }

            // Report completion
            var completeArgs = new FileOperationProgressEventArgs(
                operationId, null, totalFiles, totalFiles, FileOperationStatus.Completed, startTime: context.StartTime);
            FileOperationCompleted?.Invoke(this, completeArgs);

            _logger.LogInformation("Batch loaded {LoadedCount}/{TotalCount} STL files", loadedModels.Count, totalFiles);
            return Result<IReadOnlyList<STLModel>>.Ok(loadedModels);
        }
        finally
        {
            _activeOperations.TryRemove(operationId, out _);
        }
    }

    /// <inheritdoc/>
    public void AddToRecentFiles(Domain.ValueObjects.FileInfo fileInfo)
    {
        lock (_recentFilesLock)
        {
            // Remove if already exists
            _recentFiles.RemoveAll(f => f.FilePath.Equals(fileInfo.FilePath, StringComparison.OrdinalIgnoreCase));

            // Add to beginning
            _recentFiles.Insert(0, fileInfo);

            // Limit to max count
            while (_recentFiles.Count > MaxRecentFiles)
            {
                _recentFiles.RemoveAt(_recentFiles.Count - 1);
            }

            // Save and notify
            _ = SaveRecentFilesAsync();
            RecentFilesChanged?.Invoke(this, RecentFiles);
        }
    }

    /// <inheritdoc/>
    public void RemoveFromRecentFiles(string filePath)
    {
        lock (_recentFilesLock)
        {
            var removed = _recentFiles.RemoveAll(f => f.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                _ = SaveRecentFilesAsync();
                RecentFilesChanged?.Invoke(this, RecentFiles);
            }
        }
    }

    /// <inheritdoc/>
    public void ClearRecentFiles()
    {
        lock (_recentFilesLock)
        {
            if (_recentFiles.Count > 0)
            {
                _recentFiles.Clear();
                _ = SaveRecentFilesAsync();
                RecentFilesChanged?.Invoke(this, RecentFiles);
            }
        }
    }

    /// <inheritdoc/>
    public async Task RefreshRecentFilesAsync()
    {
        lock (_recentFilesLock)
        {
            var updated = false;

            for (int i = _recentFiles.Count - 1; i >= 0; i--)
            {
                var fileInfo = _recentFiles[i];
                var exists = File.Exists(fileInfo.FilePath);

                if (fileInfo.Exists != exists)
                {
                    _recentFiles[i] = fileInfo.WithExists(exists);
                    updated = true;
                }
            }

            if (updated)
            {
                _ = SaveRecentFilesAsync();
                RecentFilesChanged?.Invoke(this, RecentFiles);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Domain.ValueObjects.FileInfo>> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
                try
        {
            if (!File.Exists(filePath))
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail("File does not exist");
            }

            var format = await _stlParser.DetectFormatAsync(filePath, cancellationToken);
            var fileInfo = Domain.ValueObjects.FileInfo.FromPath(filePath, format);

            return Result<Domain.ValueObjects.FileInfo>.Ok(fileInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info for {FilePath}", filePath);
            return Result<Domain.ValueObjects.FileInfo>.Fail($"Error getting file info: {ex.Message}");
        }
    }

    private async Task<Result<Domain.ValueObjects.FileInfo>> ValidateSingleFileAsync(string filePath)
    {
        try
        {
            // Check if file exists
            if (!File.Exists(filePath))
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail("File does not exist");
            }

            // Check extension
            var extension = Path.GetExtension(filePath);
            if (!ValidExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail($"Invalid file extension. Expected: {string.Join(", ", ValidExtensions)}");
            }

            // Check file size
            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail("File is empty");
            }

            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail($"File is too large. Maximum size: {MaxFileSizeBytes / (1024 * 1024)}MB");
            }

            // Check if we can read the file
            try
            {
                using var stream = File.OpenRead(filePath);
                // Try to read a small portion to verify access
                var buffer = new byte[System.Math.Min(1024, (int)fileInfo.Length)];
                await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (UnauthorizedAccessException)
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail("Cannot read file: Access denied");
            }
            catch (IOException ex)
            {
                return Result<Domain.ValueObjects.FileInfo>.Fail($"Cannot read file: {ex.Message}");
            }

            // Detect format
            var format = await _stlParser.DetectFormatAsync(filePath);
            var stlFileInfo = Domain.ValueObjects.FileInfo.FromPath(filePath, format);

            return Result<Domain.ValueObjects.FileInfo>.Ok(stlFileInfo);
        }
        catch (Exception ex)
        {
            return Result<Domain.ValueObjects.FileInfo>.Fail($"Validation error: {ex.Message}");
        }
    }

    private static FileValidationErrorType DetermineErrorType(string errorMessage)
    {
        return errorMessage.ToLowerInvariant() switch
        {
            var msg when msg.Contains("does not exist") => FileValidationErrorType.FileNotFound,
            var msg when msg.Contains("invalid file extension") => FileValidationErrorType.InvalidExtension,
            var msg when msg.Contains("too large") => FileValidationErrorType.FileTooLarge,
            var msg when msg.Contains("cannot read") => FileValidationErrorType.CannotRead,
            var msg when msg.Contains("empty") => FileValidationErrorType.EmptyFile,
            _ => FileValidationErrorType.InvalidFormat
        };
    }

    private static TimeSpan? EstimateTimeRemaining(FileOperationContext context, int processedFiles, int totalFiles)
    {
        if (processedFiles == 0)
            return null;

        var elapsed = DateTime.Now - context.StartTime;
        var averageTimePerFile = elapsed.TotalMilliseconds / processedFiles;
        var remainingFiles = totalFiles - processedFiles;
        var estimatedMs = averageTimePerFile * remainingFiles;

        return TimeSpan.FromMilliseconds(estimatedMs);
    }

    private async Task LoadRecentFilesAsync()
    {
        try
        {
            if (!File.Exists(_recentFilesPath))
                return;

            var json = await File.ReadAllTextAsync(_recentFilesPath);
            var recentFilesData = JsonSerializer.Deserialize<RecentFileData[]>(json);

            if (recentFilesData != null)
            {
                lock (_recentFilesLock)
                {
                    _recentFiles.Clear();
                    foreach (var data in recentFilesData.Take(MaxRecentFiles))
                    {
                        try
                        {
                            var fileInfo = Domain.ValueObjects.FileInfo.FromPath(data.FilePath, data.Format);
                            _recentFiles.Add(fileInfo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to load recent file: {FilePath}", data.FilePath);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load recent files from {Path}", _recentFilesPath);
        }
    }

    private async Task SaveRecentFilesAsync()
    {
        try
        {
            var recentFilesData = _recentFiles.Select(f => new RecentFileData
            {
                FilePath = f.FilePath,
                Format = f.Format,
                LastAccessed = DateTime.Now
            }).ToArray();

            var json = JsonSerializer.Serialize(recentFilesData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_recentFilesPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save recent files to {Path}", _recentFilesPath);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Cancel any active operations
            foreach (var operation in _activeOperations.Values)
            {
                // Operations should handle cancellation gracefully
            }
            _activeOperations.Clear();

            _disposed = true;
        }
    }

    private class FileOperationContext
    {
        public string OperationId { get; }
        public IReadOnlyList<string> FilePaths { get; }
        public DateTime StartTime { get; }

        public FileOperationContext(string operationId, IEnumerable<string> filePaths)
        {
            OperationId = operationId;
            FilePaths = filePaths.ToList();
            StartTime = DateTime.Now;
        }
    }

    private class RecentFileData
    {
        public string FilePath { get; set; } = string.Empty;
        public STLViewer.Domain.Enums.STLFormat Format { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}
