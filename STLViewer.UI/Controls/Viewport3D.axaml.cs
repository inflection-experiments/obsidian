using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using ReactiveUI;
using Silk.NET.OpenGL;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Entities;
using STLViewer.Infrastructure.Graphics;
using STLViewer.Infrastructure.Graphics.OpenGL;
using STLViewer.Math;
using Color = STLViewer.Math.Color;

namespace STLViewer.UI.Controls;

/// <summary>
/// Custom 3D viewport control for Avalonia with camera controls and OpenGL rendering.
/// </summary>
public partial class Viewport3D : UserControl, IDisposable
{
    // OpenGL
    private OpenGlControl? _openGlControl;
    private IRenderer? _renderer;
    private ICamera? _camera;
    private STLModel? _currentModel;
    private RenderSettings _renderSettings = new();

    // Mouse/touch interaction state
    private Point _lastMousePosition;
    private bool _isOrbiting;
    private bool _isPanning;

    // Timing
    private readonly Stopwatch _frameTimer = new();
    private readonly List<double> _frameTimes = new();
    private DispatcherTimer? _renderTimer;
    private DispatcherTimer? _fpsTimer;

    // Properties
    public static readonly StyledProperty<STLModel?> ModelProperty =
        AvaloniaProperty.Register<Viewport3D, STLModel?>(nameof(Model));

    public static readonly StyledProperty<bool> ShowControlsProperty =
        AvaloniaProperty.Register<Viewport3D, bool>(nameof(ShowControls), true);

    public STLModel? Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }

    public bool ShowControls
    {
        get => GetValue(ShowControlsProperty);
        set => SetValue(ShowControlsProperty, value);
    }

    public Viewport3D()
    {
        InitializeComponent();
        InitializeRenderSettings();
        SetupEventHandlers();
        CreateOpenGLControl();
    }

    private void InitializeRenderSettings()
    {
        _renderSettings = new RenderSettings
        {
            RenderMode = Core.Interfaces.RenderMode.Surface,
            Wireframe = false,
            BackgroundColor = Color.Black,
            ModelColor = Color.LightGray,
            DepthTesting = true,
            BackfaceCulling = true,
            AntiAliasing = 4,
            Lighting = new LightingSettings
            {
                Enabled = true,
                LightDirection = new Vector3(-0.5f, -1.0f, -0.5f).Normalized(),
                DiffuseColor = Color.White,
                AmbientColor = new Color(0.2f, 0.2f, 0.2f)
            }
        };
    }

    private void SetupEventHandlers()
    {
        // Property change handlers
        this.GetObservable(ModelProperty)
            .Subscribe(OnModelChanged);

        this.GetObservable(ShowControlsProperty)
            .Subscribe(OnShowControlsChanged);

        // Control event handlers
        var resetButton = this.FindControl<Button>("ResetCameraButton");
        var frameButton = this.FindControl<Button>("FrameModelButton");
        var wireframeCheck = this.FindControl<CheckBox>("WireframeCheckBox");
        var lightingCheck = this.FindControl<CheckBox>("LightingCheckBox");
        var cullingCheck = this.FindControl<CheckBox>("BackfaceCullingCheckBox");
        var backgroundCombo = this.FindControl<ComboBox>("BackgroundComboBox");

        if (resetButton != null)
            resetButton.Click += (_, _) => ResetCamera();

        if (frameButton != null)
            frameButton.Click += (_, _) => FrameModel();

        if (wireframeCheck != null)
            wireframeCheck.IsCheckedChanged += (_, _) =>
            {
                _renderSettings.Wireframe = wireframeCheck.IsChecked ?? false;
                InvalidateVisual();
            };

        if (lightingCheck != null)
            lightingCheck.IsCheckedChanged += (_, _) =>
            {
                _renderSettings.Lighting.Enabled = lightingCheck.IsChecked ?? false;
                InvalidateVisual();
            };

        if (cullingCheck != null)
            cullingCheck.IsCheckedChanged += (_, _) =>
            {
                _renderSettings.BackfaceCulling = cullingCheck.IsChecked ?? false;
                InvalidateVisual();
            };

        if (backgroundCombo != null)
            backgroundCombo.SelectionChanged += (_, _) =>
            {
                var selectedItem = backgroundCombo.SelectedItem as ComboBoxItem;
                var colorName = selectedItem?.Content?.ToString();
                _renderSettings.BackgroundColor = colorName switch
                {
                    "White" => Color.White,
                    "Gray" => Color.Gray,
                    "Blue" => new Color(0.2f, 0.3f, 0.5f),
                    _ => Color.Black
                };
                InvalidateVisual();
            };
    }

    private void CreateOpenGLControl()
    {
        // Create the custom OpenGL control
        _openGlControl = new OpenGlControl();

        // Set up the OpenGL control
        _openGlControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        _openGlControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

        // Add event handlers
        _openGlControl.OpenGlInit += OnOpenGlInit;
        _openGlControl.OpenGlRender += OnOpenGlRender;
        _openGlControl.PointerPressed += OnPointerPressed;
        _openGlControl.PointerMoved += OnPointerMoved;
        _openGlControl.PointerReleased += OnPointerReleased;
        _openGlControl.PointerWheelChanged += OnPointerWheelChanged;
        _openGlControl.KeyDown += OnKeyDown;

        // Replace the rendering surface
        var renderingSurface = this.FindControl<Border>("RenderingSurface");
        if (renderingSurface != null)
        {
            renderingSurface.Child = _openGlControl;
        }
    }

    private void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            // Create OpenGL wrapper using Avalonia's GL interface
            var silkGl = GL.GetApi(gl.GetProcAddress);

            // Create renderer
            _renderer = RendererFactory.CreateOpenGLRenderer(silkGl);

            // Create camera
            _camera = new Infrastructure.Graphics.Camera();
            _camera.SetPerspective(45.0f, 1.0f, 0.1f, 1000.0f);
            _camera.SetPosition(new Vector3(0, 0, 5));
            _camera.SetTarget(Vector3.Zero);
            _camera.SetUp(Vector3.UnitY);

            // Initialize renderer
            Task.Run(async () =>
            {
                var width = (int)(_openGlControl?.Bounds.Width ?? 800);
                var height = (int)(_openGlControl?.Bounds.Height ?? 600);
                await _renderer.InitializeAsync(width, height);
                _renderer.SetCamera(_camera);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateStatusText("Renderer initialized");
                    UpdateRendererText(_renderer.GetInfo().Name);
                    HideLoadingText();
                    StartRenderLoop();
                });
            });
        }
        catch (Exception ex)
        {
            UpdateStatusText($"Failed to initialize OpenGL: {ex.Message}");
        }
    }

    private void OnOpenGlRender(GlInterface gl)
    {
        if (_renderer == null || _camera == null)
            return;

        _frameTimer.Restart();

        try
        {
            // Update camera aspect ratio
            var aspectRatio = (float)(_openGlControl?.Bounds.Width / _openGlControl?.Bounds.Height ?? 1.0);
            _camera.SetPerspective(45.0f, aspectRatio, 0.1f, 1000.0f);

            // Update render settings from DataContext if available
            if (DataContext is ViewModels.Viewport3DViewModel viewModel)
            {
                _renderSettings.RenderMode = viewModel.RenderMode;
                _renderSettings.Wireframe = viewModel.RenderMode == Core.Interfaces.RenderMode.Wireframe;
                _renderSettings.Lighting.Enabled = viewModel.IsLightingEnabled;
                _renderSettings.BackfaceCulling = viewModel.IsBackfaceCullingEnabled;
                _renderSettings.Material = viewModel.CurrentMaterial;
                _renderSettings.EnableTransparency = viewModel.EnableTransparency;

                // Update lighting settings to use material properties
                _renderSettings.Lighting.AmbientColor = viewModel.CurrentMaterial.AmbientColor;
                _renderSettings.Lighting.DiffuseColor = viewModel.CurrentMaterial.DiffuseColor;
                _renderSettings.Lighting.SpecularColor = viewModel.CurrentMaterial.SpecularColor;
                _renderSettings.Lighting.Shininess = viewModel.CurrentMaterial.Shininess;
            }

            // Clear the screen
            _renderer.Clear(_renderSettings.BackgroundColor);

            // Render the model if available
            if (_currentModel != null)
            {
                _renderer.Render(_currentModel, _renderSettings);
            }

            // Present the frame
            _renderer.Present();
        }
        catch (Exception ex)
        {
            UpdateStatusText($"Render error: {ex.Message}");
        }

        _frameTimer.Stop();
        UpdateFrameTime(_frameTimer.Elapsed.TotalMilliseconds);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_camera == null) return;

        _lastMousePosition = e.GetPosition(_openGlControl);
        var properties = e.GetCurrentPoint(_openGlControl).Properties;

        if (properties.IsLeftButtonPressed)
        {
            _isOrbiting = true;
            e.Pointer.Capture(_openGlControl);
        }
        else if (properties.IsRightButtonPressed)
        {
            _isPanning = true;
            e.Pointer.Capture(_openGlControl);
        }

        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_camera == null || (!_isOrbiting && !_isPanning))
            return;

        var currentPosition = e.GetPosition(_openGlControl);
        var deltaX = (float)(currentPosition.X - _lastMousePosition.X);
        var deltaY = (float)(currentPosition.Y - _lastMousePosition.Y);

        if (_isOrbiting)
        {
            // Orbit camera around target
            var sensitivity = 0.01f;
            _camera.Orbit(-deltaX * sensitivity, -deltaY * sensitivity);
        }
        else if (_isPanning)
        {
            // Pan camera
            var sensitivity = 0.01f;
            _camera.Pan(deltaX * sensitivity, -deltaY * sensitivity);
        }

        _lastMousePosition = currentPosition;
        _openGlControl?.RequestNextFrameRendering();
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isOrbiting = false;
        _isPanning = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_camera == null) return;

        var zoomFactor = e.Delta.Y > 0 ? 0.9f : 1.1f;
        _camera.Zoom(zoomFactor);

        _openGlControl?.RequestNextFrameRendering();
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.R:
                ResetCamera();
                e.Handled = true;
                break;

            case Key.F:
                FrameModel();
                e.Handled = true;
                break;
        }
    }

    private void ResetCamera()
    {
        if (_camera == null) return;

        _camera.Reset();
        _openGlControl?.RequestNextFrameRendering();
        UpdateStatusText("Camera reset");
    }

    private void FrameModel()
    {
        if (_camera == null || _currentModel == null) return;

        _camera.FrameToBounds(_currentModel.BoundingBox);
        _openGlControl?.RequestNextFrameRendering();
        UpdateStatusText("Model framed");
    }

    private void OnModelChanged(STLModel? model)
    {
        _currentModel = model;

        if (model != null)
        {
            UpdateTriangleCount(model.TriangleCount);
            UpdateStatusText($"Model loaded: {model.TriangleCount} triangles");

            // Auto-frame the model
            if (_camera != null)
            {
                _camera.FrameToBounds(model.BoundingBox);
            }
        }
        else
        {
            UpdateTriangleCount(0);
            UpdateStatusText("No model loaded");
        }

        _openGlControl?.RequestNextFrameRendering();
    }

    private void OnShowControlsChanged(bool showControls)
    {
        // Update UI visibility based on ShowControls property
        var cameraControlsOverlay = this.FindControl<Border>("CameraControlsOverlay");
        var renderSettingsOverlay = this.FindControl<Border>("RenderSettingsOverlay");

        if (cameraControlsOverlay != null)
            cameraControlsOverlay.IsVisible = showControls;

        if (renderSettingsOverlay != null)
            renderSettingsOverlay.IsVisible = showControls;
    }

    private void StartRenderLoop()
    {
        // Start render timer (60 FPS target)
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0)
        };
        _renderTimer.Tick += (_, _) => _openGlControl?.RequestNextFrameRendering();
        _renderTimer.Start();

        // Start FPS timer
        _fpsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _fpsTimer.Tick += (_, _) => UpdateFpsDisplay();
        _fpsTimer.Start();
    }

    private void UpdateFrameTime(double frameTimeMs)
    {
        _frameTimes.Add(frameTimeMs);

        // Keep only last 60 frames
        if (_frameTimes.Count > 60)
            _frameTimes.RemoveAt(0);
    }

    private void UpdateFpsDisplay()
    {
        if (_frameTimes.Count > 0)
        {
            var averageFrameTime = _frameTimes.Average();
            var fps = 1000.0 / averageFrameTime;
            UpdateFpsText($"FPS: {fps:F1}");
        }
    }

    private void UpdateStatusText(string text)
    {
        var statusText = this.FindControl<TextBlock>("StatusText");
        if (statusText != null)
            statusText.Text = text;
    }

    private void UpdateTriangleCount(int count)
    {
        var triangleText = this.FindControl<TextBlock>("TriangleCountText");
        if (triangleText != null)
            triangleText.Text = $"Triangles: {count:N0}";
    }

    private void UpdateFpsText(string text)
    {
        var fpsText = this.FindControl<TextBlock>("FpsText");
        if (fpsText != null)
            fpsText.Text = text;
    }

    private void UpdateRendererText(string text)
    {
        var rendererText = this.FindControl<TextBlock>("RendererText");
        if (rendererText != null)
            rendererText.Text = $"Renderer: {text}";
    }

    private void HideLoadingText()
    {
        var loadingText = this.FindControl<TextBlock>("LoadingText");
        if (loadingText != null)
            loadingText.IsVisible = false;
    }

    private new void InvalidateVisual()
    {
        _openGlControl?.RequestNextFrameRendering();
    }

    public void Dispose()
    {
        _renderTimer?.Stop();
        _fpsTimer?.Stop();
        _renderer?.Dispose();
    }

    /// <summary>
    /// Custom OpenGL control for Avalonia.
    /// </summary>
    private class OpenGlControl : OpenGlControlBase
    {
        public event Action<GlInterface>? OpenGlInit;
        public event Action<GlInterface>? OpenGlRender;

        protected override void OnOpenGlInit(GlInterface gl)
        {
            base.OnOpenGlInit(gl);
            OpenGlInit?.Invoke(gl);
        }

        protected override void OnOpenGlRender(GlInterface gl, int fb)
        {
            OpenGlRender?.Invoke(gl);
        }
    }
}


