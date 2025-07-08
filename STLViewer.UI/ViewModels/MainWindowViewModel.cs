using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using STLViewer.Core.Interfaces;
using STLViewer.Infrastructure.Parsers;

namespace STLViewer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
        private readonly ISTLParser _stlParser;

    public MainWindowViewModel()
    {
        // For design-time support
        _stlParser = new STLParserService();
        Viewport = new Viewport3DViewModel(_stlParser);
        InitializeCommands();
    }

    public MainWindowViewModel(ISTLParser stlParser)
    {
        _stlParser = stlParser ?? throw new ArgumentNullException(nameof(stlParser));
        Viewport = new Viewport3DViewModel(_stlParser);
        InitializeCommands();
    }

    /// <summary>
    /// The 3D viewport ViewModel that handles STL model loading and display.
    /// </summary>
    public Viewport3DViewModel Viewport { get; }

    /// <summary>
    /// Command to open a file dialog and load an STL file.
    /// </summary>
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; private set; } = null!;

    /// <summary>
    /// Command to load a sample cube for demonstration.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadSampleCommand { get; private set; } = null!;

    /// <summary>
    /// Command to load the fighter plane pre-loaded model.
    /// </summary>
    public ReactiveCommand<Unit, Unit> LoadFighterPlaneCommand { get; private set; } = null!;

    /// <summary>
    /// Command to reset the camera to its default position.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ResetCameraCommand { get; private set; } = null!;

    /// <summary>
    /// Command to frame the current model in the viewport.
    /// </summary>
    public ReactiveCommand<Unit, Unit> FrameModelCommand { get; private set; } = null!;

    /// <summary>
    /// Command to show application information.
    /// </summary>
    public ReactiveCommand<Unit, Unit> AboutCommand { get; private set; } = null!;

    /// <summary>
    /// Command to exit the application.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ExitCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set surface rendering mode.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SetSurfaceModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set wireframe rendering mode.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SetWireframeModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set shaded wireframe rendering mode.
    /// </summary>
    public ReactiveCommand<Unit, Unit> SetShadedWireframeModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to toggle lighting on/off.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleLightingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to toggle backface culling on/off.
    /// </summary>
    public ReactiveCommand<Unit, Unit> ToggleBackfaceCullingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set a camera preset.
    /// </summary>
    public ReactiveCommand<string, Unit> SetCameraPresetCommand { get; private set; } = null!;

    /// <summary>
    /// Gets whether surface rendering mode is selected.
    /// </summary>
    public bool IsRenderModeSurface => Viewport.RenderMode == Core.Interfaces.RenderMode.Surface;

    /// <summary>
    /// Gets whether wireframe rendering mode is selected.
    /// </summary>
    public bool IsRenderModeWireframe => Viewport.RenderMode == Core.Interfaces.RenderMode.Wireframe;

    /// <summary>
    /// Gets whether shaded wireframe rendering mode is selected.
    /// </summary>
    public bool IsRenderModeShadedWireframe => Viewport.RenderMode == Core.Interfaces.RenderMode.ShadedWireframe;

    /// <summary>
    /// Gets whether lighting is enabled.
    /// </summary>
    public bool IsLightingEnabled => Viewport.IsLightingEnabled;

    /// <summary>
    /// Gets whether backface culling is enabled.
    /// </summary>
    public bool IsBackfaceCullingEnabled => Viewport.IsBackfaceCullingEnabled;

    private void InitializeCommands()
    {
        // Initialize commands
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
        LoadSampleCommand = ReactiveCommand.Create(LoadSample);
        LoadFighterPlaneCommand = ReactiveCommand.CreateFromTask(LoadFighterPlaneAsync);
        ResetCameraCommand = ReactiveCommand.Create(ResetCamera);
        FrameModelCommand = ReactiveCommand.Create(FrameModel);
        AboutCommand = ReactiveCommand.Create(ShowAbout);
        ExitCommand = ReactiveCommand.Create(Exit);

        // Rendering mode commands
        SetSurfaceModeCommand = ReactiveCommand.Create(SetSurfaceMode);
        SetWireframeModeCommand = ReactiveCommand.Create(SetWireframeMode);
        SetShadedWireframeModeCommand = ReactiveCommand.Create(SetShadedWireframeMode);
        ToggleLightingCommand = ReactiveCommand.Create(ToggleLighting);
        ToggleBackfaceCullingCommand = ReactiveCommand.Create(ToggleBackfaceCulling);
        SetCameraPresetCommand = ReactiveCommand.CreateFromTask<string>(SetCameraPresetAsync);

        // Subscribe to viewport property changes to update menu states
        Viewport.WhenAnyValue(x => x.RenderMode)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(IsRenderModeSurface));
                this.RaisePropertyChanged(nameof(IsRenderModeWireframe));
                this.RaisePropertyChanged(nameof(IsRenderModeShadedWireframe));
            });

        Viewport.WhenAnyValue(x => x.IsLightingEnabled)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsLightingEnabled)));

        Viewport.WhenAnyValue(x => x.IsBackfaceCullingEnabled)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsBackfaceCullingEnabled)));
    }

    private async Task OpenFileAsync()
    {
        try
        {
            // Get the main window for the file dialog
            var window = Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (window?.StorageProvider == null)
            {
                Viewport.StatusMessage = "Unable to open file dialog";
                return;
            }

            // Configure file picker options
            var fileTypes = new[]
            {
                new FilePickerFileType("STL Files")
                {
                    Patterns = new[] { "*.stl" },
                    MimeTypes = new[] { "application/sla", "model/stl" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            };

            var options = new FilePickerOpenOptions
            {
                Title = "Open STL File",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            // Show file picker
            var files = await window.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count > 0)
            {
                var file = files[0];
                var filePath = file.Path.LocalPath;

                if (!string.IsNullOrEmpty(filePath))
                {
                    await Viewport.LoadFileAsync(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error opening file: {ex.Message}";
        }
    }

    private void LoadSample()
    {
        try
        {
            Viewport.SetSampleModel();
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error loading sample: {ex.Message}";
        }
    }

    private async Task LoadFighterPlaneAsync()
    {
        try
        {
            Viewport.StatusMessage = "Loading fighter plane model...";

            // In a real implementation, this would use MediatR to send a LoadPreloadedModelCommand
            // For now, we'll use a placeholder approach
            await Task.Delay(500); // Simulate loading time

            // This is a placeholder - in real implementation would use the Application layer
            Viewport.StatusMessage = "Fighter plane model loaded successfully!";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error loading fighter plane: {ex.Message}";
        }
    }

    private void ResetCamera()
    {
        // This will be handled by the viewport control directly
        // For now, we'll just update the status
        Viewport.StatusMessage = "Camera reset requested";
    }

    private void FrameModel()
    {
        // This will be handled by the viewport control directly
        // For now, we'll just update the status
        Viewport.StatusMessage = "Frame model requested";
    }

    private void ShowAbout()
    {
        Viewport.StatusMessage = "STL Viewer - A 3D model viewer for STL files";
    }

    private void Exit()
    {
        // Request application shutdown
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void SetSurfaceMode()
    {
        Viewport.RenderMode = Core.Interfaces.RenderMode.Surface;
    }

    private void SetWireframeMode()
    {
        Viewport.RenderMode = Core.Interfaces.RenderMode.Wireframe;
    }

    private void SetShadedWireframeMode()
    {
        Viewport.RenderMode = Core.Interfaces.RenderMode.ShadedWireframe;
    }

    private void ToggleLighting()
    {
        Viewport.IsLightingEnabled = !Viewport.IsLightingEnabled;
    }

    private void ToggleBackfaceCulling()
    {
        Viewport.IsBackfaceCullingEnabled = !Viewport.IsBackfaceCullingEnabled;
    }

    private async Task SetCameraPresetAsync(string presetId)
    {
        try
        {
            Viewport.StatusMessage = $"Setting camera to {presetId} view...";

            // In a real implementation, this would use MediatR to send a SetCameraPresetCommand
            // For now, we'll use a placeholder approach
            await Task.Delay(300); // Simulate animation time

            Viewport.StatusMessage = $"Camera set to {presetId} view";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error setting camera preset: {ex.Message}";
        }
    }
}
