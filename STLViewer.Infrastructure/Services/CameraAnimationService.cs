using STLViewer.Core.Interfaces;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;
using System.Diagnostics;

namespace STLViewer.Infrastructure.Services;

/// <summary>
/// Service for animating camera transitions with smooth easing.
/// </summary>
public class CameraAnimationService : ICameraAnimationService
{
    private readonly object _lockObject = new();
    private CancellationTokenSource? _animationCancellation;
    private bool _isAnimating;
    private float _animationProgress;

    /// <inheritdoc/>
    public bool IsAnimating
    {
        get
        {
            lock (_lockObject)
            {
                return _isAnimating;
            }
        }
    }

    /// <inheritdoc/>
    public float AnimationProgress
    {
        get
        {
            lock (_lockObject)
            {
                return _animationProgress;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<float>? AnimationProgressChanged;

    /// <inheritdoc/>
    public event EventHandler<bool>? AnimationCompleted;

    /// <inheritdoc/>
    public async Task<Result> AnimateToCameraPresetAsync(
        ICamera camera,
        CameraPreset targetPreset,
        int durationMs,
        CancellationToken cancellationToken = default)
    {
        if (camera == null)
            return Result.Fail("Camera cannot be null");

        if (targetPreset == null)
            return Result.Fail("Target preset cannot be null");

        if (durationMs <= 0)
            return Result.Fail("Duration must be positive");

        // Stop any existing animation
        StopAnimation();

        try
        {
            // Create new cancellation token source
            lock (_lockObject)
            {
                _animationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _isAnimating = true;
                _animationProgress = 0.0f;
            }

            // Store initial camera state
            var startPosition = camera.Position;
            var startTarget = camera.Target;
            var startUp = camera.Up;
            var startFov = camera.FieldOfView;

            // Target camera state
            var endPosition = targetPreset.Position;
            var endTarget = targetPreset.Target;
            var endUp = targetPreset.Up;
            var endFov = targetPreset.FieldOfView * MathF.PI / 180.0f; // Convert degrees to radians

            // Animation timing
            var stopwatch = Stopwatch.StartNew();
            var duration = TimeSpan.FromMilliseconds(durationMs);

            // Animation loop
            while (stopwatch.Elapsed < duration)
            {
                _animationCancellation.Token.ThrowIfCancellationRequested();

                // Calculate interpolation factor (0 to 1)
                var t = (float)(stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
                t = System.Math.Clamp(t, 0.0f, 1.0f);

                // Apply easing function (smooth step)
                var easedT = SmoothStep(t);

                // Interpolate camera parameters
                var currentPosition = Vector3.Lerp(startPosition, endPosition, easedT);
                var currentTarget = Vector3.Lerp(startTarget, endTarget, easedT);
                var currentUp = Vector3.Lerp(startUp, endUp, easedT);
                var currentFov = Lerp(startFov, endFov, easedT);

                // Apply interpolated values
                camera.SetPosition(currentPosition);
                camera.SetTarget(currentTarget);
                camera.SetUp(currentUp.Normalized());
                camera.SetFieldOfView(currentFov);

                // Update progress
                lock (_lockObject)
                {
                    _animationProgress = t;
                }

                // Notify progress change
                AnimationProgressChanged?.Invoke(this, t);

                // Small delay to allow rendering (~60 FPS)
                await Task.Delay(16, _animationCancellation.Token);
            }

            // Ensure final position is exact
            camera.SetPosition(endPosition);
            camera.SetTarget(endTarget);
            camera.SetUp(endUp.Normalized());
            camera.SetFieldOfView(endFov);

            // Mark as completed
            lock (_lockObject)
            {
                _isAnimating = false;
                _animationProgress = 1.0f;
                _animationCancellation?.Dispose();
                _animationCancellation = null;
            }

            // Notify completion
            AnimationCompleted?.Invoke(this, true);

            return Result.Ok();
        }
        catch (OperationCanceledException)
        {
            // Animation was cancelled
            lock (_lockObject)
            {
                _isAnimating = false;
                _animationCancellation?.Dispose();
                _animationCancellation = null;
            }

            AnimationCompleted?.Invoke(this, false);
            return Result.Fail("Animation was cancelled");
        }
        catch (Exception ex)
        {
            // Animation failed
            lock (_lockObject)
            {
                _isAnimating = false;
                _animationCancellation?.Dispose();
                _animationCancellation = null;
            }

            AnimationCompleted?.Invoke(this, false);
            return Result.Fail($"Animation failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Result StopAnimation()
    {
        try
        {
            lock (_lockObject)
            {
                if (_animationCancellation != null)
                {
                    _animationCancellation.Cancel();
                    _animationCancellation.Dispose();
                    _animationCancellation = null;
                }

                _isAnimating = false;
                _animationProgress = 0.0f;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to stop animation: {ex.Message}");
        }
    }

    /// <summary>
    /// Applies smooth step easing function.
    /// </summary>
    /// <param name="t">The interpolation parameter (0 to 1).</param>
    /// <returns>The eased value.</returns>
    private static float SmoothStep(float t)
    {
        // Smooth step function: 3t² - 2t³
        return t * t * (3.0f - 2.0f * t);
    }

    /// <summary>
    /// Linear interpolation between two values.
    /// </summary>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="t">The interpolation parameter (0 to 1).</param>
    /// <returns>The interpolated value.</returns>
    private static float Lerp(float start, float end, float t)
    {
        return start + (end - start) * t;
    }
}
