using STLViewer.Math;

namespace STLViewer.Domain.ValueObjects;

/// <summary>
/// Represents a layer for organizing scene objects.
/// </summary>
public sealed record Layer
{
    /// <summary>
    /// Gets the unique identifier for this layer.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the layer.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the description of the layer.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether this layer is visible.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether objects in this layer are selectable.
    /// </summary>
    public bool IsSelectable { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether objects in this layer are locked from modification.
    /// </summary>
    public bool IsLocked { get; init; } = false;

    /// <summary>
    /// Gets the color associated with this layer for display purposes.
    /// </summary>
    public Color Color { get; init; } = Color.Gray;

    /// <summary>
    /// Gets the opacity of the layer (0.0 = transparent, 1.0 = opaque).
    /// </summary>
    public float Opacity { get; init; } = 1.0f;

    /// <summary>
    /// Gets a value indicating whether this is the default layer.
    /// </summary>
    public bool IsDefault { get; init; } = false;

    /// <summary>
    /// Gets the order/index of this layer for display purposes.
    /// </summary>
    public int Order { get; init; } = 0;

    /// <summary>
    /// Gets the timestamp when the layer was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    private Layer() { }

    /// <summary>
    /// Creates a new layer.
    /// </summary>
    /// <param name="name">The name of the layer.</param>
    /// <param name="description">The description of the layer.</param>
    /// <param name="color">The color of the layer.</param>
    /// <param name="isVisible">Whether the layer is visible.</param>
    /// <param name="isSelectable">Whether objects in the layer are selectable.</param>
    /// <param name="isLocked">Whether the layer is locked.</param>
    /// <param name="opacity">The opacity of the layer.</param>
    /// <param name="order">The display order of the layer.</param>
    /// <returns>A new layer instance.</returns>
    public static Layer Create(
        string name,
        string description = "",
        Color? color = null,
        bool isVisible = true,
        bool isSelectable = true,
        bool isLocked = false,
        float opacity = 1.0f,
        int order = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Layer name cannot be null or empty.", nameof(name));

        if (opacity < 0.0f || opacity > 1.0f)
            throw new ArgumentException("Opacity must be between 0.0 and 1.0.", nameof(opacity));

        return new Layer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Color = color ?? Color.Gray,
            IsVisible = isVisible,
            IsSelectable = isSelectable,
            IsLocked = isLocked,
            Opacity = opacity,
            Order = order,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates the default layer.
    /// </summary>
    /// <returns>The default layer.</returns>
    public static Layer CreateDefault()
    {
        return new Layer
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            Description = "Default layer for all objects",
            Color = Color.White,
            IsVisible = true,
            IsSelectable = true,
            IsLocked = false,
            Opacity = 1.0f,
            Order = 0,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a new layer with modified properties.
    /// </summary>
    /// <param name="name">The new name (optional).</param>
    /// <param name="description">The new description (optional).</param>
    /// <param name="color">The new color (optional).</param>
    /// <param name="isVisible">The new visibility (optional).</param>
    /// <param name="isSelectable">The new selectability (optional).</param>
    /// <param name="isLocked">The new locked state (optional).</param>
    /// <param name="opacity">The new opacity (optional).</param>
    /// <param name="order">The new order (optional).</param>
    /// <returns>A new layer instance with the specified changes.</returns>
    public Layer With(
        string? name = null,
        string? description = null,
        Color? color = null,
        bool? isVisible = null,
        bool? isSelectable = null,
        bool? isLocked = null,
        float? opacity = null,
        int? order = null)
    {
        if (opacity.HasValue && (opacity.Value < 0.0f || opacity.Value > 1.0f))
            throw new ArgumentException("Opacity must be between 0.0 and 1.0.", nameof(opacity));

        return this with
        {
            Name = name ?? Name,
            Description = description ?? Description,
            Color = color ?? Color,
            IsVisible = isVisible ?? IsVisible,
            IsSelectable = isSelectable ?? IsSelectable,
            IsLocked = isLocked ?? IsLocked,
            Opacity = opacity ?? Opacity,
            Order = order ?? Order
        };
    }

    /// <summary>
    /// Toggles the visibility of this layer.
    /// </summary>
    /// <returns>A new layer instance with toggled visibility.</returns>
    public Layer ToggleVisibility()
    {
        return this with { IsVisible = !IsVisible };
    }

    /// <summary>
    /// Toggles the locked state of this layer.
    /// </summary>
    /// <returns>A new layer instance with toggled locked state.</returns>
    public Layer ToggleLocked()
    {
        return this with { IsLocked = !IsLocked };
    }

    /// <summary>
    /// Toggles the selectability of this layer.
    /// </summary>
    /// <returns>A new layer instance with toggled selectability.</returns>
    public Layer ToggleSelectable()
    {
        return this with { IsSelectable = !IsSelectable };
    }
}
