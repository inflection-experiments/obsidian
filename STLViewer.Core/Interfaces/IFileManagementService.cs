using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Service for managing STL file operations including drag-and-drop, recent files, and batch loading.
/// </summary>
public interface IFileManagementService
{
    /// <summary>
    /// Gets the list of recently opened files.
    /// </summary>
    IReadOnlyList<Domain.ValueObjects.FileInfo> RecentFiles { get; }

    /// <summary>
    /// Gets the maximum number of recent files to track.
    /// </summary>
    int MaxRecentFiles { get; set; }

    /// <summary>
    /// Event raised when the recent files list changes.
    /// </summary>
    event EventHandler<IReadOnlyList<Domain.ValueObjects.FileInfo>>? RecentFilesChanged;

    /// <summary>
    /// Event raised when a file operation begins.
    /// </summary>
    event EventHandler<FileOperationProgressEventArgs>? FileOperationStarted;

    /// <summary>
    /// Event raised when file operation progress changes.
    /// </summary>
    event EventHandler<FileOperationProgressEventArgs>? FileOperationProgress;

    /// <summary>
    /// Event raised when a file operation completes.
    /// </summary>
    event EventHandler<FileOperationProgressEventArgs>? FileOperationCompleted;

    /// <summary>
    /// Validates if the given file paths are valid STL files.
    /// </summary>
    /// <param name="filePaths">The file paths to validate.</param>
    /// <returns>A result containing validation information.</returns>
    Task<Result<FileValidationResult>> ValidateFilesAsync(IEnumerable<string> filePaths);

    /// <summary>
    /// Loads a single STL file.
    /// </summary>
    /// <param name="filePath">The path to the STL file.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the loaded STL model.</returns>
    Task<Result<STLModel>> LoadFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads multiple STL files in batch.
    /// </summary>
    /// <param name="filePaths">The paths to the STL files.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the loaded STL models.</returns>
    Task<Result<IReadOnlyList<STLModel>>> LoadFilesAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a file to the recent files list.
    /// </summary>
    /// <param name="fileInfo">The file information to add.</param>
    void AddToRecentFiles(Domain.ValueObjects.FileInfo fileInfo);

    /// <summary>
    /// Removes a file from the recent files list.
    /// </summary>
    /// <param name="filePath">The file path to remove.</param>
    void RemoveFromRecentFiles(string filePath);

    /// <summary>
    /// Clears all recent files.
    /// </summary>
    void ClearRecentFiles();

    /// <summary>
    /// Checks if recent files still exist and updates their status.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshRecentFilesAsync();

    /// <summary>
    /// Gets file information for a given path.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the file information.</returns>
    Task<Result<Domain.ValueObjects.FileInfo>> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for file operation progress.
/// </summary>
public class FileOperationProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the operation identifier.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the current file being processed.
    /// </summary>
    public string? CurrentFile { get; }

    /// <summary>
    /// Gets the total number of files to process.
    /// </summary>
    public int TotalFiles { get; }

    /// <summary>
    /// Gets the number of files processed so far.
    /// </summary>
    public int ProcessedFiles { get; }

    /// <summary>
    /// Gets the progress percentage (0-100).
    /// </summary>
    public double ProgressPercentage { get; }

    /// <summary>
    /// Gets the operation status.
    /// </summary>
    public FileOperationStatus Status { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the time when the operation started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the estimated time remaining.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; }

    public FileOperationProgressEventArgs(
        string operationId,
        string? currentFile,
        int totalFiles,
        int processedFiles,
        FileOperationStatus status,
        string? errorMessage = null,
        DateTime? startTime = null,
        TimeSpan? estimatedTimeRemaining = null)
    {
        OperationId = operationId;
        CurrentFile = currentFile;
        TotalFiles = totalFiles;
        ProcessedFiles = processedFiles;
        ProgressPercentage = totalFiles > 0 ? (double)processedFiles / totalFiles * 100 : 0;
        Status = status;
        ErrorMessage = errorMessage;
        StartTime = startTime ?? DateTime.Now;
        EstimatedTimeRemaining = estimatedTimeRemaining;
    }
}

/// <summary>
/// Status of a file operation.
/// </summary>
public enum FileOperationStatus
{
    /// <summary>
    /// Operation is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// Operation is in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Operation completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Operation was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Operation failed with an error.
    /// </summary>
    Failed
}

/// <summary>
/// Result of file validation.
/// </summary>
public class FileValidationResult
{
    /// <summary>
    /// Gets the list of valid files.
    /// </summary>
    public IReadOnlyList<Domain.ValueObjects.FileInfo> ValidFiles { get; }

    /// <summary>
    /// Gets the list of invalid files with error messages.
    /// </summary>
    public IReadOnlyList<FileValidationError> InvalidFiles { get; }

    /// <summary>
    /// Gets whether all files are valid.
    /// </summary>
    public bool AllValid => InvalidFiles.Count == 0;

    /// <summary>
    /// Gets the total number of files validated.
    /// </summary>
    public int TotalFiles => ValidFiles.Count + InvalidFiles.Count;

    public FileValidationResult(
        IReadOnlyList<Domain.ValueObjects.FileInfo> validFiles,
        IReadOnlyList<FileValidationError> invalidFiles)
    {
        ValidFiles = validFiles;
        InvalidFiles = invalidFiles;
    }
}

/// <summary>
/// Represents a file validation error.
/// </summary>
public class FileValidationError
{
    /// <summary>
    /// Gets the file path that failed validation.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the validation error message.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Gets the error type.
    /// </summary>
    public FileValidationErrorType ErrorType { get; }

    public FileValidationError(string filePath, string errorMessage, FileValidationErrorType errorType)
    {
        FilePath = filePath;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
    }
}

/// <summary>
/// Types of file validation errors.
/// </summary>
public enum FileValidationErrorType
{
    /// <summary>
    /// File does not exist.
    /// </summary>
    FileNotFound,

    /// <summary>
    /// Invalid file extension.
    /// </summary>
    InvalidExtension,

    /// <summary>
    /// File is too large.
    /// </summary>
    FileTooLarge,

    /// <summary>
    /// File cannot be read (permissions, etc.).
    /// </summary>
    CannotRead,

    /// <summary>
    /// File format is invalid or corrupted.
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// File is empty.
    /// </summary>
    EmptyFile
}
