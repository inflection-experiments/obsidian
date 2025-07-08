using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Interface for generating pre-loaded STL models.
/// </summary>
public interface IPreloadedModelGenerator
{
    /// <summary>
    /// Gets the unique identifier for this model generator.
    /// </summary>
    string ModelId { get; }

    /// <summary>
    /// Gets the display name for this model.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description for this model.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the category for this model.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the estimated triangle count for this model.
    /// </summary>
    int EstimatedTriangleCount { get; }

    /// <summary>
    /// Gets the estimated file size in bytes for this model.
    /// </summary>
    long EstimatedSizeBytes { get; }

    /// <summary>
    /// Gets the tags associated with this model.
    /// </summary>
    IEnumerable<string> Tags { get; }

    /// <summary>
    /// Generates the STL model.
    /// </summary>
    /// <returns>A result containing the generated STL model.</returns>
    Result<STLModel> GenerateModel();
}
