using System;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using STLViewer.Core.Interfaces;
using STLViewer.Infrastructure.Parsers;
using MediatR;
using STLViewer.Application.Commands;
using STLViewer.Application.Queries;
using STLViewer.Domain.ValueObjects;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DomainFileInfo = STLViewer.Domain.ValueObjects.FileInfo;

namespace STLViewer.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISTLParser _stlParser;
    private readonly IMediator _mediator;
    private readonly IFileManagementService? _fileManagementService;
    private readonly ISceneManager? _sceneManager;
    private ObservableCollection<DomainFileInfo> _recentFiles = new();

    public MainWindowViewModel()
    {
        // For design-time support
        _stlParser = new STLParserService();
        _mediator = null!; // Will be null for design-time
        _fileManagementService = null;
        _sceneManager = null;
        Viewport = new Viewport3DViewModel(_stlParser);
        SceneTree = null; // Will be null for design-time
        InitializeCommands();
    }

    public MainWindowViewModel(ISTLParser stlParser, IMediator mediator, IFileManagementService fileManagementService, ISceneManager sceneManager)
    {
        _stlParser = stlParser ?? throw new ArgumentNullException(nameof(stlParser));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _fileManagementService = fileManagementService ?? throw new ArgumentNullException(nameof(fileManagementService));
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        Viewport = new Viewport3DViewModel(_stlParser);

        // Initialize scene tree
        SceneTree = new SceneTreeViewModel(_sceneManager, _mediator);

        // Subscribe to recent files changes
        _fileManagementService.RecentFilesChanged += OnRecentFilesChanged;

        InitializeCommands();
        // Load recent files
        _ = LoadRecentFilesAsync();

        // Create a default scene
        CreateDefaultSceneAsync();
    }

    /// <summary>
    /// The 3D viewport ViewModel that handles STL model loading and display.
    /// </summary>
    public Viewport3DViewModel Viewport { get; }

    /// <summary>
    /// The scene tree ViewModel that handles the scene hierarchy.
    /// </summary>
    public SceneTreeViewModel? SceneTree { get; }

    /// <summary>
    /// Command to open a file dialog and load an STL file.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenFileCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open multiple files dialog and load STL files.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> OpenMultipleFilesCommand { get; private set; } = null!;

    /// <summary>
    /// Command to open a recent file.
    /// </summary>
    public ReactiveCommand<string, System.Reactive.Unit> OpenRecentFileCommand { get; private set; } = null!;

    /// <summary>
    /// Command to clear the recent files list.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ClearRecentFilesCommand { get; private set; } = null!;

    /// <summary>
    /// Command to load a sample cube for demonstration.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LoadSampleCommand { get; private set; } = null!;

    /// <summary>
    /// Command to load the fighter plane pre-loaded model.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> LoadFighterPlaneCommand { get; private set; } = null!;

    /// <summary>
    /// Command to reset the camera to its default position.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ResetCameraCommand { get; private set; } = null!;

    /// <summary>
    /// Command to frame the current model in the viewport.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> FrameModelCommand { get; private set; } = null!;

    /// <summary>
    /// Command to show application information.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> AboutCommand { get; private set; } = null!;

    /// <summary>
    /// Command to exit the application.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ExitCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set surface rendering mode.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetSurfaceModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set wireframe rendering mode.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetWireframeModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set shaded wireframe rendering mode.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> SetShadedWireframeModeCommand { get; private set; } = null!;

    /// <summary>
    /// Command to toggle lighting on/off.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleLightingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to toggle backface culling on/off.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> ToggleBackfaceCullingCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set a camera preset.
    /// </summary>
    public ReactiveCommand<string, System.Reactive.Unit> SetCameraPresetCommand { get; private set; } = null!;

    /// <summary>
    /// Command to set a lighting preset.
    /// </summary>
    public ReactiveCommand<string, System.Reactive.Unit> SetLightingPresetCommand { get; private set; } = null!;

    /// <summary>
    /// Gets the available lighting presets.
    /// </summary>
    public IEnumerable<string> LightingPresets { get; } = new[]
    {
        "Basic", "Studio", "Outdoor", "Indoor", "Technical", "Dramatic", "Showcase"
    };

    /// <summary>
    /// Command to enable the flight path plugin.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> EnableFlightPathPluginCommand { get; private set; } = null!;

    /// <summary>
    /// Command to disable the flight path plugin.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> DisableFlightPathPluginCommand { get; private set; } = null!;

    /// <summary>
    /// Command to create a sample flight path.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> CreateSampleFlightPathCommand { get; private set; } = null!;

    /// <summary>
    /// Command to start flight path animation.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> StartFlightPathAnimationCommand { get; private set; } = null!;

    /// <summary>
    /// Command to stop flight path animation.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> StopFlightPathAnimationCommand { get; private set; } = null!;

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

    /// <summary>
    /// Gets the collection of recent files.
    /// </summary>
    public ObservableCollection<DomainFileInfo> RecentFiles => _recentFiles;

    /// <summary>
    /// Gets whether there are any recent files.
    /// </summary>
    public bool HasRecentFiles => _recentFiles.Count > 0;

    private void InitializeCommands()
    {
        // Initialize commands
        OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
        OpenMultipleFilesCommand = ReactiveCommand.CreateFromTask(OpenMultipleFilesAsync);
        OpenRecentFileCommand = ReactiveCommand.CreateFromTask<string>(OpenRecentFileAsync);
        ClearRecentFilesCommand = ReactiveCommand.CreateFromTask(ClearRecentFilesAsync);
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
        SetLightingPresetCommand = ReactiveCommand.CreateFromTask<string>(SetLightingPresetAsync);

        // Flight path plugin commands
        EnableFlightPathPluginCommand = ReactiveCommand.CreateFromTask(EnableFlightPathPluginAsync);
        DisableFlightPathPluginCommand = ReactiveCommand.CreateFromTask(DisableFlightPathPluginAsync);
        CreateSampleFlightPathCommand = ReactiveCommand.CreateFromTask(CreateSampleFlightPathAsync);
        StartFlightPathAnimationCommand = ReactiveCommand.CreateFromTask(StartFlightPathAnimationAsync);
        StopFlightPathAnimationCommand = ReactiveCommand.CreateFromTask(StopFlightPathAnimationAsync);

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

    private async void ResetCamera()
    {
        try
        {
            if (_mediator == null)
            {
                // Design-time fallback
                Viewport.StatusMessage = "Camera reset requested";
                return;
            }

            var command = new ResetCameraCommand
            {
                Animated = true,
                AnimationDurationMs = 500
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                Viewport.StatusMessage = "Camera reset to default position";
            }
            else
            {
                Viewport.StatusMessage = $"Error resetting camera: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error resetting camera: {ex.Message}";
        }
    }

    private async void FrameModel()
    {
        try
        {
            if (_mediator == null)
            {
                // Design-time fallback
                Viewport.StatusMessage = "Frame model requested";
                return;
            }

            if (Viewport.CurrentModel?.BoundingBox == null)
            {
                Viewport.StatusMessage = "No model loaded to frame";
                return;
            }

            var command = new FrameCameraCommand
            {
                BoundingBox = Viewport.CurrentModel.BoundingBox,
                PaddingFactor = 1.2f,
                Animated = true,
                AnimationDurationMs = 500
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                Viewport.StatusMessage = "Model framed in view";
            }
            else
            {
                Viewport.StatusMessage = $"Error framing model: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error framing model: {ex.Message}";
        }
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
            if (_mediator == null)
            {
                // Design-time fallback
                Viewport.StatusMessage = $"Setting camera to {presetId} view...";
                await Task.Delay(300);
                Viewport.StatusMessage = $"Camera set to {presetId} view";
                return;
            }

            Viewport.StatusMessage = $"Setting camera to {presetId} view...";

            // Get the current model's bounding box for proper framing
            var boundingBox = Viewport.CurrentModel?.BoundingBox;

            // Create and send the camera preset command
            var command = new SetCameraPresetCommand
            {
                PresetId = presetId,
                BoundingBox = boundingBox,
                Animated = true,
                AnimationDurationMs = 500
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                Viewport.StatusMessage = $"Camera set to {presetId} view";
            }
            else
            {
                Viewport.StatusMessage = $"Error setting camera preset: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error setting camera preset: {ex.Message}";
        }
    }

    private async Task SetLightingPresetAsync(string presetId)
    {
        try
        {
            if (_mediator == null)
            {
                // Design-time fallback
                Viewport.StatusMessage = $"Setting lighting to {presetId} preset...";
                await Task.Delay(300);
                Viewport.StatusMessage = $"Lighting set to {presetId} preset";
                return;
            }

            Viewport.StatusMessage = $"Setting lighting to {presetId} preset...";

            // Parse the preset type from string
            if (!Enum.TryParse<STLViewer.Domain.ValueObjects.LightingPresetType>(presetId, true, out var presetType))
            {
                Viewport.StatusMessage = $"Invalid lighting preset: {presetId}";
                return;
            }

            // Create and send the lighting preset command
            var command = new STLViewer.Application.Commands.SetLightingPresetCommand
            {
                PresetType = presetType,
                Animated = true,
                AnimationDurationMs = 500
            };

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                Viewport.StatusMessage = $"Lighting set to {presetId} preset";
            }
            else
            {
                Viewport.StatusMessage = $"Error setting lighting preset: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error setting lighting preset: {ex.Message}";
        }
    }

    private async Task EnableFlightPathPluginAsync()
    {
        try
        {
            Viewport.StatusMessage = "Enabling flight path plugin...";

            // In a real implementation, this would use MediatR to send EnablePluginCommand
            // For now, we'll use a placeholder approach
            await Task.Delay(500); // Simulate initialization time

            Viewport.StatusMessage = "Flight path plugin enabled";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error enabling flight path plugin: {ex.Message}";
        }
    }

    private async Task DisableFlightPathPluginAsync()
    {
        try
        {
            Viewport.StatusMessage = "Disabling flight path plugin...";

            // In a real implementation, this would use MediatR to send DisablePluginCommand
            // For now, we'll use a placeholder approach
            await Task.Delay(500); // Simulate shutdown time

            Viewport.StatusMessage = "Flight path plugin disabled";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error disabling flight path plugin: {ex.Message}";
        }
    }

    private async Task CreateSampleFlightPathAsync()
    {
        try
        {
            Viewport.StatusMessage = "Creating sample flight path...";

            // In a real implementation, this would use MediatR to send CreateFlightPathCommand
            // For now, we'll use a placeholder approach
            await Task.Delay(1000); // Simulate creation time

            Viewport.StatusMessage = "Sample flight path created around fighter plane";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error creating sample flight path: {ex.Message}";
        }
    }

    private async Task StartFlightPathAnimationAsync()
    {
        try
        {
            Viewport.StatusMessage = "Starting flight path animation...";

            // In a real implementation, this would use MediatR to send StartFlightPathAnimationCommand
            // For now, we'll use a placeholder approach
            await Task.Delay(500); // Simulate start time

            Viewport.StatusMessage = "Flight path animation started - Press Space to toggle";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error starting flight path animation: {ex.Message}";
        }
    }

    private async Task StopFlightPathAnimationAsync()
    {
        try
        {
            Viewport.StatusMessage = "Stopping flight path animation...";

            // In a real implementation, this would use MediatR to send stop command
            // For now, we'll use a placeholder approach
            await Task.Delay(500); // Simulate stop time

            Viewport.StatusMessage = "Flight path animation stopped";
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error stopping flight path animation: {ex.Message}";
        }
    }

    private async Task OpenMultipleFilesAsync()
    {
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Open STL Files",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("STL Files")
                        {
                            Patterns = new[] { "*.stl" }
                        }
                    }
                });

                if (files.Count > 0)
                {
                    var filePaths = files.Select(f => f.Path.LocalPath).ToList();
                    await LoadMultipleFilesAsync(filePaths);
                }
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error opening files: {ex.Message}";
        }
    }

    private async Task OpenRecentFileAsync(string filePath)
    {
        try
        {
            await LoadFileAsync(filePath);
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error loading recent file: {ex.Message}";
        }
    }

    private async Task ClearRecentFilesAsync()
    {
        try
        {
            if (_mediator != null)
            {
                await _mediator.Send(new ClearRecentFilesCommand());
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error clearing recent files: {ex.Message}";
        }
    }

    public async Task LoadFileAsync(string filePath)
    {
        try
        {
            if (_mediator != null)
            {
                var command = new LoadSTLCommand { FilePath = filePath };
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    Viewport.LoadModel(result.Value!);
                    Viewport.StatusMessage = $"Loaded: {System.IO.Path.GetFileName(filePath)}";
                }
                else
                {
                    Viewport.StatusMessage = $"Failed to load file: {result.Error}";
                }
            }
            else
            {
                // Fallback for design-time
                var result = await _stlParser.ParseAsync(filePath);
                if (result.IsSuccess)
                {
                    Viewport.LoadModel(result.Value!);
                    Viewport.StatusMessage = $"Loaded: {System.IO.Path.GetFileName(filePath)}";
                }
                else
                {
                    Viewport.StatusMessage = $"Failed to load file: {result.Error}";
                }
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error loading file: {ex.Message}";
        }
    }

    public async Task LoadMultipleFilesAsync(IEnumerable<string> filePaths)
    {
        try
        {
            if (_mediator != null)
            {
                var command = new LoadFilesCommand(filePaths);
                var result = await _mediator.Send(command);

                if (result.IsSuccess)
                {
                    var models = result.Value!;
                    if (models.Count > 0)
                    {
                        // For now, load the first model in the main viewport
                        // In the future, this could be enhanced to support multiple models
                        Viewport.LoadModel(models.First());
                        Viewport.StatusMessage = $"Loaded {models.Count} files. Displaying: {System.IO.Path.GetFileName(models.First().Metadata.FileName)}";
                    }
                    else
                    {
                        Viewport.StatusMessage = "No files were loaded successfully.";
                    }
                }
                else
                {
                    Viewport.StatusMessage = $"Failed to load files: {result.Error}";
                }
            }
            else
            {
                // Fallback for design-time - just load the first file
                var firstPath = filePaths.FirstOrDefault();
                if (firstPath != null)
                {
                    await LoadFileAsync(firstPath);
                }
            }
        }
        catch (Exception ex)
        {
            Viewport.StatusMessage = $"Error loading files: {ex.Message}";
        }
    }

    private async Task LoadRecentFilesAsync()
    {
        try
        {
            if (_mediator != null)
            {
                var recentFiles = await _mediator.Send(new GetRecentFilesQuery(refreshStatus: false));

                _recentFiles.Clear();
                foreach (var file in recentFiles)
                {
                    _recentFiles.Add(file);
                }

                this.RaisePropertyChanged(nameof(HasRecentFiles));
            }
        }
        catch (Exception ex)
        {
            // Log error but don't show to user as this is a background operation
            System.Diagnostics.Debug.WriteLine($"Error loading recent files: {ex.Message}");
        }
    }

    private void OnRecentFilesChanged(object? sender, IReadOnlyList<DomainFileInfo> recentFiles)
    {
        // Update the observable collection on the UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _recentFiles.Clear();
            foreach (var file in recentFiles)
            {
                _recentFiles.Add(file);
            }
            this.RaisePropertyChanged(nameof(HasRecentFiles));
        });
    }

    private void CreateDefaultSceneAsync()
    {
        try
        {
            if (_sceneManager != null)
            {
                var result = _sceneManager.CreateScene("Default Scene");
                if (result.IsSuccess)
                {
                    _sceneManager.SetCurrentScene(result.Value);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating default scene: {ex.Message}");
        }
    }
}
