using System.Text;
using Microsoft.Extensions.Logging;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;
using STLViewer.Domain.Enums;

namespace STLViewer.Infrastructure.Parsers;

/// <summary>
/// Main STL parser service that detects format and routes to appropriate parser.
/// </summary>
public class STLParserService : ISTLParser
{
    private readonly AsciiSTLParser _asciiParser;
    private readonly BinarySTLParser _binaryParser;
    private readonly ILogger<STLParserService>? _logger;

    public STLParserService(ILogger<STLParserService>? logger = null)
    {
        _asciiParser = new AsciiSTLParser();
        _binaryParser = new BinarySTLParser();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<STLModel>> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return Result<STLModel>.Fail($"File not found: {filePath}");

            var fileName = Path.GetFileName(filePath);
            var data = await File.ReadAllBytesAsync(filePath, cancellationToken);

            return await ParseAsync(data, fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading STL file: {FilePath}", filePath);
            return Result<STLModel>.Fail($"Error reading file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<STLModel>> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, cancellationToken);
            var data = memoryStream.ToArray();

            return await ParseAsync(data, fileName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reading STL stream for file: {FileName}", fileName);
            return Result<STLModel>.Fail($"Error reading stream: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<STLModel>> ParseAsync(byte[] data, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            if (data == null || data.Length == 0)
                return Result<STLModel>.Fail("No data provided");

            _logger?.LogInformation("Parsing STL file: {FileName} ({Size} bytes)", fileName, data.Length);

            // Detect format
            var format = DetectFormat(data);
            _logger?.LogDebug("Detected STL format: {Format} for file: {FileName}", format, fileName);

            // Parse using appropriate parser
            Result<STLModel> result = format switch
            {
                STLFormat.ASCII => await _asciiParser.ParseAsync(data, fileName, cancellationToken),
                STLFormat.Binary => await _binaryParser.ParseAsync(data, fileName, cancellationToken),
                STLFormat.Unknown => await TryBothParsers(data, fileName, cancellationToken),
                _ => Result<STLModel>.Fail($"Unsupported STL format: {format}")
            };

            if (result.IsSuccess)
            {
                _logger?.LogInformation("Successfully parsed STL file: {FileName} with {TriangleCount} triangles",
                    fileName, result.Value.TriangleCount);
            }
            else
            {
                _logger?.LogWarning("Failed to parse STL file: {FileName}. Error: {Error}", fileName, result.Error);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error parsing STL file: {FileName}", fileName);
            return Result<STLModel>.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<STLFormat> DetectFormatAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
                return STLFormat.Unknown;

            // Read first part of file for detection
            const int sampleSize = 1024;
            var buffer = new byte[sampleSize];

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var bytesRead = await fileStream.ReadAsync(buffer, 0, sampleSize, cancellationToken);

            if (bytesRead == 0)
                return STLFormat.Unknown;

            // For more accurate detection, we might need the full file
            if (bytesRead == sampleSize && fileStream.Length > sampleSize)
            {
                // Read entire file for better detection
                fileStream.Seek(0, SeekOrigin.Begin);
                var fullData = new byte[fileStream.Length];
                await fileStream.ReadAsync(fullData, 0, (int)fileStream.Length, cancellationToken);
                return DetectFormat(fullData);
            }

            // Resize buffer to actual bytes read
            Array.Resize(ref buffer, bytesRead);
            return DetectFormat(buffer);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error detecting STL format for file: {FilePath}", filePath);
            return STLFormat.Unknown;
        }
    }

    /// <inheritdoc />
    public STLFormat DetectFormat(byte[] data)
    {
        if (data == null || data.Length == 0)
            return STLFormat.Unknown;

        try
        {
            // Quick check for ASCII format - look for "solid" at the beginning
            if (data.Length >= 5)
            {
                var text = Encoding.UTF8.GetString(data, 0, System.Math.Min(100, data.Length)).ToLowerInvariant();
                var startsWithSolid = text.TrimStart().StartsWith("solid");

                // If it starts with "solid", it could be ASCII, but we need to verify
                if (startsWithSolid)
                {
                    // Check if it's actually a binary file with "solid" in the header
                    if (BinarySTLParser.IsValidBinarySTL(data))
                    {
                        // It's a valid binary file, but starts with "solid"
                        // Use additional heuristics to determine the actual format
                        return DetectFormatWithHeuristics(data);
                    }
                    else
                    {
                        // Not a valid binary file, likely ASCII
                        return STLFormat.ASCII;
                    }
                }
            }

            // Check if it's a valid binary STL
            if (BinarySTLParser.IsValidBinarySTL(data))
            {
                return STLFormat.Binary;
            }

            // If we get here, try to detect using content analysis
            return AnalyzeContentForFormat(data);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during STL format detection");
            return STLFormat.Unknown;
        }
    }

    private STLFormat DetectFormatWithHeuristics(byte[] data)
    {
        // This handles the case where a binary file starts with "solid"
        // Use multiple heuristics to determine the actual format

        // Check if binary format makes sense
        if (BinarySTLParser.IsValidBinarySTL(data))
        {
            var triangleCount = BinarySTLParser.GetTriangleCount(data);
            if (triangleCount.HasValue && triangleCount > 0)
            {
                var expectedSize = 80 + 4 + (triangleCount.Value * 50);
                if (System.Math.Abs(data.Length - expectedSize) <= 2) // Allow small discrepancy
                {
                    return STLFormat.Binary;
                }
            }
        }

        // Check ASCII characteristics
        var text = Encoding.UTF8.GetString(data, 0, System.Math.Min(1000, data.Length));
        var asciiIndicators = 0;

        if (text.Contains("facet", StringComparison.OrdinalIgnoreCase)) asciiIndicators++;
        if (text.Contains("vertex", StringComparison.OrdinalIgnoreCase)) asciiIndicators++;
        if (text.Contains("endloop", StringComparison.OrdinalIgnoreCase)) asciiIndicators++;
        if (text.Contains("endfacet", StringComparison.OrdinalIgnoreCase)) asciiIndicators++;
        if (text.Contains("endsolid", StringComparison.OrdinalIgnoreCase)) asciiIndicators++;

        return asciiIndicators >= 2 ? STLFormat.ASCII : STLFormat.Binary;
    }

    private STLFormat AnalyzeContentForFormat(byte[] data)
    {
        // Analyze the content to guess the format
        try
        {
            // Check if data looks like text (ASCII)
            var sampleSize = System.Math.Min(1000, data.Length);
            var text = Encoding.UTF8.GetString(data, 0, sampleSize);

            // Count printable characters
            var printableChars = text.Count(c => !char.IsControl(c) || char.IsWhiteSpace(c));
            var printableRatio = (double)printableChars / sampleSize;

            // If mostly printable characters, likely ASCII
            if (printableRatio > 0.8)
            {
                // Check for ASCII STL keywords
                var lowerText = text.ToLowerInvariant();
                if (lowerText.Contains("solid") || lowerText.Contains("facet") || lowerText.Contains("vertex"))
                {
                    return STLFormat.ASCII;
                }
            }

            // If not clearly ASCII, assume binary
            return STLFormat.Binary;
        }
        catch
        {
            return STLFormat.Unknown;
        }
    }

    private async Task<Result<STLModel>> TryBothParsers(byte[] data, string fileName, CancellationToken cancellationToken)
    {
        _logger?.LogWarning("Format detection uncertain for {FileName}, trying both parsers", fileName);

        // Try binary first (usually faster to fail)
        var binaryResult = await _binaryParser.ParseAsync(data, fileName, cancellationToken);
        if (binaryResult.IsSuccess)
        {
            _logger?.LogInformation("Successfully parsed {FileName} as binary STL", fileName);
            return binaryResult;
        }

        _logger?.LogDebug("Binary parsing failed for {FileName}, trying ASCII. Error: {Error}",
            fileName, binaryResult.Error);

        // Try ASCII
        var asciiResult = await _asciiParser.ParseAsync(data, fileName, cancellationToken);
        if (asciiResult.IsSuccess)
        {
            _logger?.LogInformation("Successfully parsed {FileName} as ASCII STL", fileName);
            return asciiResult;
        }

        _logger?.LogDebug("ASCII parsing also failed for {FileName}. Error: {Error}",
            fileName, asciiResult.Error);

        // Both failed, return the more informative error
        var combinedError = $"Failed to parse as binary STL: {binaryResult.Error}. " +
                           $"Failed to parse as ASCII STL: {asciiResult.Error}";

        return Result<STLModel>.Fail(combinedError);
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(STLModel model, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result.Fail("Model cannot be null");

            if (string.IsNullOrEmpty(filePath))
                return Result.Fail("File path cannot be null or empty");

            _logger?.LogInformation("Saving STL model to: {FilePath} in {Format} format", filePath, model.Metadata.Format);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save using appropriate parser based on format
            Result saveResult = model.Metadata.Format switch
            {
                STLFormat.ASCII => await _asciiParser.SaveAsync(model, filePath, cancellationToken),
                STLFormat.Binary => await _binaryParser.SaveAsync(model, filePath, cancellationToken),
                _ => Result.Fail($"Unsupported STL format for saving: {model.Metadata.Format}")
            };

            if (saveResult.IsSuccess)
            {
                _logger?.LogInformation("Successfully saved STL model: {FilePath} with {TriangleCount} triangles",
                    filePath, model.TriangleCount);
            }
            else
            {
                _logger?.LogError("Failed to save STL model: {FilePath}. Error: {Error}", filePath, saveResult.Error);
            }

            return saveResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error saving STL model: {FilePath}", filePath);
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(STLModel model, Stream stream, CancellationToken cancellationToken = default)
    {
        try
        {
            if (model == null)
                return Result.Fail("Model cannot be null");

            if (stream == null)
                return Result.Fail("Stream cannot be null");

            if (!stream.CanWrite)
                return Result.Fail("Stream must be writable");

            _logger?.LogInformation("Saving STL model to stream in {Format} format", model.Metadata.Format);

            // Save using appropriate parser based on format
            Result saveResult = model.Metadata.Format switch
            {
                STLFormat.ASCII => await _asciiParser.SaveAsync(model, stream, cancellationToken),
                STLFormat.Binary => await _binaryParser.SaveAsync(model, stream, cancellationToken),
                _ => Result.Fail($"Unsupported STL format for saving: {model.Metadata.Format}")
            };

            if (saveResult.IsSuccess)
            {
                _logger?.LogInformation("Successfully saved STL model to stream with {TriangleCount} triangles",
                    model.TriangleCount);
            }
            else
            {
                _logger?.LogError("Failed to save STL model to stream. Error: {Error}", saveResult.Error);
            }

            return saveResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error saving STL model to stream");
            return Result.Fail($"Unexpected error: {ex.Message}");
        }
    }
}
