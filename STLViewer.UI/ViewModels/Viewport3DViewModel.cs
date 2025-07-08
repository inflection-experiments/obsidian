using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Entities;
using STLViewer.Infrastructure.Parsers;

namespace STLViewer.UI.ViewModels;

/// <summary>
/// ViewModel for the 3D viewport control with STL model loading and display capabilities.
/// </summary>
public class Viewport3DViewModel : ViewModelBase
{
    private readonly ISTLParser _stlParser;

    private STLModel? _currentModel;
    private bool _isLoading;
    private string _statusMessage = "Ready";
    private string? _currentFilePath;
    private bool _showControls = true;
    private Core.Interfaces.RenderMode _renderMode = Core.Interfaces.RenderMode.Surface;
    private bool _isLightingEnabled = true;
    private bool _isBackfaceCullingEnabled = true;
    private Core.Interfaces.Material _currentMaterial = Core.Interfaces.Material.FromPreset(Core.Interfaces.MaterialPreset.Default);
    private Core.Interfaces.MaterialPreset _materialPreset = Core.Interfaces.MaterialPreset.Default;
    private float _transparency = 1.0f;
    private bool _enableTransparency = false;

    public Viewport3DViewModel(ISTLParser stlParser)
    {
        _stlParser = stlParser;

        // Initialize material system
        _currentMaterial = Core.Interfaces.Material.FromPreset(Core.Interfaces.MaterialPreset.Default);
        _transparency = _currentMaterial.Alpha;

        // Subscribe to material changes to update transparency automatically
        this.WhenAnyValue(x => x.CurrentMaterial)
            .Subscribe(material =>
            {
                if (material != null && System.Math.Abs(_transparency - material.Alpha) > 0.001f)
                {
                    _transparency = material.Alpha;
                    this.RaisePropertyChanged(nameof(Transparency));
                    this.RaisePropertyChanged(nameof(MaterialColorHex));
                }
            });

        // Initialize commands
        LoadFileCommand = ReactiveCommand.CreateFromTask(LoadFileAsync);
        ReloadFileCommand = ReactiveCommand.CreateFromTask(ReloadFileAsync, this.WhenAnyValue(x => x.CurrentFilePath, path => !string.IsNullOrEmpty(path)));
        ClearModelCommand = ReactiveCommand.Create(ClearModel);

        // Subscribe to command execution feedback
        LoadFileCommand.IsExecuting.Subscribe(isExecuting => IsLoading = isExecuting);
        ReloadFileCommand.IsExecuting.Subscribe(isExecuting => IsLoading = isExecuting);

        LoadFileCommand.ThrownExceptions.Subscribe(ex =>
            StatusMessage = $"Error loading file: {ex.Message}");

        ReloadFileCommand.ThrownExceptions.Subscribe(ex =>
            StatusMessage = $"Error reloading file: {ex.Message}");
    }

    /// <summary>
    /// The currently loaded STL model.
    /// </summary>
    public STLModel? CurrentModel
    {
        get => _currentModel;
        private set => this.RaiseAndSetIfChanged(ref _currentModel, value);
    }

    /// <summary>
    /// Indicates whether a file loading operation is in progress.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
    }

    /// <summary>
    /// Current status message for display in the UI.
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Path to the currently loaded file.
    /// </summary>
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        private set => this.RaiseAndSetIfChanged(ref _currentFilePath, value);
    }

    /// <summary>
    /// Whether to show viewport controls overlay.
    /// </summary>
    public bool ShowControls
    {
        get => _showControls;
        set => this.RaiseAndSetIfChanged(ref _showControls, value);
    }

    /// <summary>
    /// The current rendering mode.
    /// </summary>
    public Core.Interfaces.RenderMode RenderMode
    {
        get => _renderMode;
        set => this.RaiseAndSetIfChanged(ref _renderMode, value);
    }

    /// <summary>
    /// Whether lighting is enabled.
    /// </summary>
    public bool IsLightingEnabled
    {
        get => _isLightingEnabled;
        set => this.RaiseAndSetIfChanged(ref _isLightingEnabled, value);
    }

    /// <summary>
    /// Whether backface culling is enabled.
    /// </summary>
    public bool IsBackfaceCullingEnabled
    {
        get => _isBackfaceCullingEnabled;
        set => this.RaiseAndSetIfChanged(ref _isBackfaceCullingEnabled, value);
    }

    /// <summary>
    /// The current material properties.
    /// </summary>
    public Core.Interfaces.Material CurrentMaterial
    {
        get => _currentMaterial;
        set => this.RaiseAndSetIfChanged(ref _currentMaterial, value);
    }

    /// <summary>
    /// The current material preset.
    /// </summary>
    public Core.Interfaces.MaterialPreset MaterialPreset
    {
        get => _materialPreset;
        set
        {
            var oldValue = _materialPreset;
            this.RaiseAndSetIfChanged(ref _materialPreset, value);

            // Update material when preset changes
            if (oldValue != value && value != Core.Interfaces.MaterialPreset.Custom)
            {
                var baseColor = _currentMaterial.DiffuseColor;
                CurrentMaterial = Core.Interfaces.Material.FromPreset(value, baseColor);
            }
        }
    }

    /// <summary>
    /// The transparency level (0.0 = fully transparent, 1.0 = fully opaque).
    /// </summary>
    public float Transparency
    {
        get => _transparency;
        set
        {
            var oldValue = _transparency;
            this.RaiseAndSetIfChanged(ref _transparency, value);

            if (oldValue != value)
            {
                _currentMaterial.Alpha = value;
                this.RaisePropertyChanged(nameof(CurrentMaterial));
            }
        }
    }

    /// <summary>
    /// Whether transparency is enabled.
    /// </summary>
    public bool EnableTransparency
    {
        get => _enableTransparency;
        set => this.RaiseAndSetIfChanged(ref _enableTransparency, value);
    }

    /// <summary>
    /// Gets the available material presets.
    /// </summary>
    public Core.Interfaces.MaterialPreset[] MaterialPresets { get; } = Enum.GetValues<Core.Interfaces.MaterialPreset>();

    /// <summary>
    /// Gets file information for the currently loaded model.
    /// </summary>
    public string? FileInfo
    {
        get
        {
            if (CurrentModel == null || string.IsNullOrEmpty(CurrentFilePath))
                return null;

            var fileInfo = new FileInfo(CurrentFilePath);
            return $"{Path.GetFileName(CurrentFilePath)} ({fileInfo.Length / 1024.0:F1} KB)";
        }
    }

    /// <summary>
    /// Gets model statistics for display.
    /// </summary>
    public string? ModelInfo
    {
        get
        {
            if (CurrentModel == null)
                return null;

            var bounds = CurrentModel.BoundingBox;
            var size = bounds.Size;

            return $"Triangles: {CurrentModel.TriangleCount:N0}, " +
                   $"Size: {size.X:F2} × {size.Y:F2} × {size.Z:F2}";
        }
    }

    /// <summary>
    /// Command to load an STL file from disk.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFileCommand { get; }

    /// <summary>
    /// Command to reload the current file.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ReloadFileCommand { get; }

    /// <summary>
    /// Command to clear the current model.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ClearModelCommand { get; }

    /// <summary>
    /// Loads an STL file from the specified path.
    /// </summary>
    /// <param name="filePath">Path to the STL file to load.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task LoadFileAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                StatusMessage = "File not found";
                return;
            }

            StatusMessage = "Loading STL file...";

            var result = await _stlParser.ParseAsync(filePath);

            if (result.IsSuccess && result.Value != null)
            {
                CurrentModel = result.Value;
                CurrentFilePath = filePath;
                StatusMessage = $"Loaded {CurrentModel.TriangleCount:N0} triangles";

                // Trigger property change notifications for computed properties
                this.RaisePropertyChanged(nameof(FileInfo));
                this.RaisePropertyChanged(nameof(ModelInfo));
            }
            else
            {
                StatusMessage = $"Failed to load STL file: {result.Error}";
                CurrentModel = null;
                CurrentFilePath = null;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading file: {ex.Message}";
            CurrentModel = null;
            CurrentFilePath = null;
        }
    }

    /// <summary>
    /// Loads an STL file using a file dialog (placeholder for UI integration).
    /// </summary>
    private Task LoadFileAsync()
    {
        // This would typically open a file dialog
        // For now, we'll use a sample file path or throw an exception
        // The actual file dialog implementation would be handled by the View
        throw new NotImplementedException("File dialog integration should be handled by the View");
    }

    /// <summary>
    /// Reloads the current STL file.
    /// </summary>
    private async Task ReloadFileAsync()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            await LoadFileAsync(CurrentFilePath);
        }
    }

    /// <summary>
    /// Clears the current model.
    /// </summary>
    private void ClearModel()
    {
        CurrentModel = null;
        CurrentFilePath = null;
        StatusMessage = "Model cleared";

        this.RaisePropertyChanged(nameof(FileInfo));
        this.RaisePropertyChanged(nameof(ModelInfo));
    }

    /// <summary>
    /// Sets a sample STL model for testing purposes.
    /// </summary>
    public void SetSampleModel()
    {
        try
        {
            // Create a simple test cube
            var triangles = CreateTestCube();

            // Create sample STL data for the cube
            var sampleData = CreateSampleSTLData();

            var result = STLModel.CreateFromTriangles(
                fileName: "Sample Cube",
                triangles: triangles,
                rawData: sampleData,
                format: Domain.Enums.STLFormat.ASCII);

            if (result.IsSuccess && result.Value != null)
            {
                CurrentModel = result.Value;
                CurrentFilePath = null;
                StatusMessage = "Sample cube loaded";

                this.RaisePropertyChanged(nameof(FileInfo));
                this.RaisePropertyChanged(nameof(ModelInfo));
            }
            else
            {
                StatusMessage = $"Error creating sample model: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating sample model: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates sample STL data for the test cube.
    /// </summary>
    private static byte[] CreateSampleSTLData()
    {
        // Create minimal ASCII STL data
        var stlContent = @"solid TestCube
facet normal 0.0 0.0 -1.0
  outer loop
    vertex -1.0 -1.0 -1.0
    vertex  1.0 -1.0 -1.0
    vertex  1.0  1.0 -1.0
  endloop
endfacet
endsolid TestCube";

        return System.Text.Encoding.UTF8.GetBytes(stlContent);
    }

    /// <summary>
    /// Updates the material's diffuse color and switches to custom preset if needed.
    /// </summary>
    /// <param name="color">The new diffuse color.</param>
    public void SetMaterialColor(STLViewer.Math.Color color)
    {
        var material = _currentMaterial.Clone();
        material.DiffuseColor = color;

        // If we're not already on custom preset, switch to it
        if (_materialPreset != Core.Interfaces.MaterialPreset.Custom)
        {
            material.Preset = Core.Interfaces.MaterialPreset.Custom;
            _materialPreset = Core.Interfaces.MaterialPreset.Custom;
            this.RaisePropertyChanged(nameof(MaterialPreset));
        }

        CurrentMaterial = material;
    }

    /// <summary>
    /// Gets the current material color as a hex string for UI display.
    /// </summary>
    public string MaterialColorHex =>
        $"#{(int)(_currentMaterial.DiffuseColor.R * 255):X2}{(int)(_currentMaterial.DiffuseColor.G * 255):X2}{(int)(_currentMaterial.DiffuseColor.B * 255):X2}";

    /// <summary>
    /// Sets the material transparency and enables transparency mode if needed.
    /// </summary>
    /// <param name="alpha">The alpha value (0.0 to 1.0).</param>
    public void SetMaterialTransparency(float alpha)
    {
        Transparency = System.Math.Clamp(alpha, 0.0f, 1.0f);

        // Auto-enable transparency if alpha is less than 1
        if (alpha < 1.0f && !_enableTransparency)
        {
            EnableTransparency = true;
        }
    }

    /// <summary>
    /// Creates a simple test cube model.
    /// </summary>
    private static IReadOnlyList<Domain.ValueObjects.Triangle> CreateTestCube()
    {
        var triangles = new List<Domain.ValueObjects.Triangle>();

        // Define cube vertices
        var vertices = new[]
        {
            new Math.Vector3(-1, -1, -1), // 0
            new Math.Vector3( 1, -1, -1), // 1
            new Math.Vector3( 1,  1, -1), // 2
            new Math.Vector3(-1,  1, -1), // 3
            new Math.Vector3(-1, -1,  1), // 4
            new Math.Vector3( 1, -1,  1), // 5
            new Math.Vector3( 1,  1,  1), // 6
            new Math.Vector3(-1,  1,  1)  // 7
        };

        // Define cube faces (each face = 2 triangles)
        var faces = new[]
        {
            // Front face (z = -1)
            new[] { 0, 1, 2 }, new[] { 0, 2, 3 },
            // Back face (z = 1)
            new[] { 5, 4, 7 }, new[] { 5, 7, 6 },
            // Left face (x = -1)
            new[] { 4, 0, 3 }, new[] { 4, 3, 7 },
            // Right face (x = 1)
            new[] { 1, 5, 6 }, new[] { 1, 6, 2 },
            // Bottom face (y = -1)
            new[] { 4, 5, 1 }, new[] { 4, 1, 0 },
            // Top face (y = 1)
            new[] { 3, 2, 6 }, new[] { 3, 6, 7 }
        };

        foreach (var face in faces)
        {
            var v1 = vertices[face[0]];
            var v2 = vertices[face[1]];
            var v3 = vertices[face[2]];

            var triangle = Domain.ValueObjects.Triangle.Create(v1, v2, v3);
            triangles.Add(triangle);
        }

        return triangles;
    }
}
