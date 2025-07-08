using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Core.Interfaces;

/// <summary>
/// Service for animating camera transitions.
/// </summary>
public interface ICameraAnimationService
{
    /// <summary>
    /// Gets a value indicating whether a camera animation is currently running.
    /// </summary>
    bool IsAnimating { get; }

    /// <summary>
    /// Gets the current animation progress (0.0 to 1.0).
    /// </summary>
    float AnimationProgress { get; }

    /// <summary>
    /// Animates the camera to a target preset.
    /// </summary>
    /// <param name="camera">The camera to animate.</param>
    /// <param name="targetPreset">The target camera preset.</param>
    /// <param name="durationMs">The animation duration in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token for the animation.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> AnimateToCameraPresetAsync(
        ICamera camera,
        CameraPreset targetPreset,
        int durationMs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops any currently running animation.
    /// </summary>
    /// <returns>A result indicating success or failure.</returns>
    Result StopAnimation();

    /// <summary>
    /// Event raised when animation progress changes.
    /// </summary>
    event EventHandler<float>? AnimationProgressChanged;

    /// <summary>
    /// Event raised when animation completes.
    /// </summary>
    event EventHandler<bool>? AnimationCompleted;
}
