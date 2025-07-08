using STLViewer.Domain.Enums;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents information about a file in the STL viewer.
/// </summary>
public sealed class FileInfo : IEquatable<FileInfo>
{
    /// <summary>
    /// Gets the full file path.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the file name without path.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the file name without extension.
    /// </summary>
    public string FileNameWithoutExtension { get; }

    /// <summary>
    /// Gets the file extension.
    /// </summary>
    public string Extension { get; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long SizeInBytes { get; }

    /// <summary>
    /// Gets the last modified time.
    /// </summary>
    public DateTime LastModified { get; }

    /// <summary>
    /// Gets the STL format type.
    /// </summary>
    public STLFormat Format { get; }

    /// <summary>
    /// Gets whether the file currently exists on disk.
    /// </summary>
    public bool Exists { get; }

    /// <summary>
    /// Gets a human-readable file size string.
    /// </summary>
    public string FormattedSize { get; }

    private FileInfo(
        string filePath,
        string fileName,
        string fileNameWithoutExtension,
        string extension,
        long sizeInBytes,
        DateTime lastModified,
        STLFormat format,
        bool exists)
    {
        FilePath = filePath;
        FileName = fileName;
        FileNameWithoutExtension = fileNameWithoutExtension;
        Extension = extension;
        SizeInBytes = sizeInBytes;
        LastModified = lastModified;
        Format = format;
        Exists = exists;
        FormattedSize = FormatFileSize(sizeInBytes);
    }

    /// <summary>
    /// Creates a FileInfo instance from a file path.
    /// </summary>
    /// <param name="filePath">The file path to analyze.</param>
    /// <param name="format">The detected STL format.</param>
    /// <returns>A new FileInfo instance.</returns>
    public static FileInfo FromPath(string filePath, STLFormat format = STLFormat.Unknown)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var fileName = Path.GetFileName(filePath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        // Get file information if it exists
        bool exists = File.Exists(filePath);
        long sizeInBytes = 0;
        DateTime lastModified = DateTime.MinValue;

        if (exists)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            sizeInBytes = fileInfo.Length;
            lastModified = fileInfo.LastWriteTime;
        }

        return new FileInfo(
            filePath,
            fileName,
            fileNameWithoutExtension,
            extension,
            sizeInBytes,
            lastModified,
            format,
            exists);
    }

    /// <summary>
    /// Creates a FileInfo instance with updated existence status.
    /// </summary>
    /// <param name="exists">The new existence status.</param>
    /// <returns>A new FileInfo instance with updated status.</returns>
    public FileInfo WithExists(bool exists)
    {
        if (this.Exists == exists)
            return this;

        // If we're checking and the file exists, update size and modified time
        long sizeInBytes = this.SizeInBytes;
        DateTime lastModified = this.LastModified;

        if (exists && File.Exists(FilePath))
        {
            var fileInfo = new System.IO.FileInfo(FilePath);
            sizeInBytes = fileInfo.Length;
            lastModified = fileInfo.LastWriteTime;
        }

        return new FileInfo(
            FilePath,
            FileName,
            FileNameWithoutExtension,
            Extension,
            sizeInBytes,
            lastModified,
            Format,
            exists);
    }

    /// <summary>
    /// Creates a FileInfo instance with updated format.
    /// </summary>
    /// <param name="format">The new STL format.</param>
    /// <returns>A new FileInfo instance with updated format.</returns>
    public FileInfo WithFormat(STLFormat format)
    {
        return new FileInfo(
            FilePath,
            FileName,
            FileNameWithoutExtension,
            Extension,
            SizeInBytes,
            LastModified,
            format,
            Exists);
    }

    /// <summary>
    /// Gets the relative age of the file.
    /// </summary>
    public TimeSpan Age => DateTime.Now - LastModified;

    /// <summary>
    /// Checks if the file is considered large (> 10MB).
    /// </summary>
    public bool IsLargeFile => SizeInBytes > 10 * 1024 * 1024;

    /// <summary>
    /// Checks if the file has a valid STL extension.
    /// </summary>
    public bool HasValidExtension =>
        string.Equals(Extension, ".stl", StringComparison.OrdinalIgnoreCase);

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (System.Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }

    /// <inheritdoc/>
    public bool Equals(FileInfo? other)
    {
        return other != null &&
               string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as FileInfo);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return FilePath.ToLowerInvariant().GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{FileName} ({FormattedSize})";
    }

    public static bool operator ==(FileInfo? left, FileInfo? right)
    {
        return EqualityComparer<FileInfo>.Default.Equals(left, right);
    }

    public static bool operator !=(FileInfo? left, FileInfo? right)
    {
        return !(left == right);
    }
}
