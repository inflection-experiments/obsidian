using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for parsing STL files.
/// </summary>
public interface ISTLParser
{
    /// <summary>
    /// Parses an STL file from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the STL file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the parsed STL model or error information.</returns>
    Task<Result<STLModel>> ParseAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses an STL file from a stream.
    /// </summary>
    /// <param name="stream">The stream containing STL data.</param>
    /// <param name="fileName">The original filename for metadata.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the parsed STL model or error information.</returns>
    Task<Result<STLModel>> ParseAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses an STL file from byte array.
    /// </summary>
    /// <param name="data">The STL file data.</param>
    /// <param name="fileName">The original filename for metadata.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the parsed STL model or error information.</returns>
    Task<Result<STLModel>> ParseAsync(byte[] data, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the format of an STL file.
    /// </summary>
    /// <param name="filePath">The path to the STL file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The detected STL format.</returns>
    Task<Domain.Enums.STLFormat> DetectFormatAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the format of STL data.
    /// </summary>
    /// <param name="data">The STL file data.</param>
    /// <returns>The detected STL format.</returns>
    Domain.Enums.STLFormat DetectFormat(byte[] data);

    /// <summary>
    /// Saves an STL model to the specified file path.
    /// </summary>
    /// <param name="model">The STL model to save.</param>
    /// <param name="filePath">The path where to save the STL file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result indicating success or failure of the save operation.</returns>
    Task<Result> SaveAsync(STLModel model, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves an STL model to a stream.
    /// </summary>
    /// <param name="model">The STL model to save.</param>
    /// <param name="stream">The stream to write the STL data to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result indicating success or failure of the save operation.</returns>
    Task<Result> SaveAsync(STLModel model, Stream stream, CancellationToken cancellationToken = default);
}
