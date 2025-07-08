using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get available camera presets.
/// </summary>
public class GetCameraPresetsQuery : IRequest<Result<IEnumerable<CameraPresetInfo>>>
{
    /// <summary>
    /// Gets or sets the optional bounding box to adjust presets for.
    /// </summary>
    public BoundingBox? BoundingBox { get; set; }
}

/// <summary>
/// Information about a camera preset.
/// </summary>
public class CameraPresetInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the preset.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the preset.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the preset.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the keyboard shortcut for the preset.
    /// </summary>
    public string? KeyboardShortcut { get; set; }

    /// <summary>
    /// Gets or sets the camera position.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Gets or sets the camera target.
    /// </summary>
    public Vector3 Target { get; set; }

    /// <summary>
    /// Gets or sets the up vector.
    /// </summary>
    public Vector3 Up { get; set; }

    /// <summary>
    /// Gets or sets the field of view.
    /// </summary>
    public float FieldOfView { get; set; }

    /// <summary>
    /// Gets or sets whether this preset is orthographic.
    /// </summary>
    public bool IsOrthographic { get; set; }
}

/// <summary>
/// Handler for GetCameraPresetsQuery.
/// </summary>
public class GetCameraPresetsQueryHandler : IRequestHandler<GetCameraPresetsQuery, Result<IEnumerable<CameraPresetInfo>>>
{
    public async Task<Result<IEnumerable<CameraPresetInfo>>> Handle(GetCameraPresetsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get standard camera presets
            var presets = CameraPreset.GetStandardPresets();

            // Adjust for bounding box if provided
            if (request.BoundingBox.HasValue)
            {
                presets = presets.Select(p => p.AdjustForBoundingBox(request.BoundingBox.Value));
            }

            // Convert to DTOs
            var presetInfos = presets.Select(preset => new CameraPresetInfo
            {
                Id = preset.Id,
                DisplayName = preset.DisplayName,
                Description = preset.Description,
                KeyboardShortcut = preset.KeyboardShortcut,
                Position = preset.Position,
                Target = preset.Target,
                Up = preset.Up,
                FieldOfView = preset.FieldOfView,
                IsOrthographic = IsOrthographicPreset(preset.Id)
            });

            return await Task.FromResult(Result<IEnumerable<CameraPresetInfo>>.Ok(presetInfos));
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<CameraPresetInfo>>.Fail($"Failed to get camera presets: {ex.Message}");
        }
    }

    private bool IsOrthographicPreset(string presetId)
    {
        // Orthographic presets are typically the orthogonal views
        return presetId.ToLowerInvariant() switch
        {
            "front" => true,
            "back" => true,
            "top" => true,
            "bottom" => true,
            "left" => true,
            "right" => true,
            "isometric" => false,
            "perspective" => false,
            _ => false
        };
    }
}
