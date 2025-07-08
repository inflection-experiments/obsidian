namespace STLViewer.Domain.Events;

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets the date and time when this event occurred.
    /// </summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when an STL model is successfully loaded.
/// </summary>
public sealed record STLModelLoadedEvent(
    Guid ModelId,
    string FileName,
    int TriangleCount,
    Domain.Enums.STLFormat Format,
    TimeSpan LoadDuration) : DomainEvent;

/// <summary>
/// Event raised when an STL model is transformed.
/// </summary>
public sealed record STLModelTransformedEvent(
    Guid ModelId,
    string TransformationType,
    System.Numerics.Matrix4x4 TransformMatrix) : DomainEvent;

/// <summary>
/// Event raised when an STL model's metadata is updated.
/// </summary>
public sealed record STLModelMetadataUpdatedEvent(
    Guid ModelId,
    ValueObjects.ModelMetadata PreviousMetadata,
    ValueObjects.ModelMetadata NewMetadata) : DomainEvent;
