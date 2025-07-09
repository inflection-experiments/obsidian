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
using STLViewer.Infrastructure.Parsers;

namespace STLViewer.UI.Controls;

/// <summary>
/// 3D viewport control for displaying STL models using OpenGL rendering.
/// </summary>
public partial class Viewport3D : UserControl
{
    // OpenGL
    private OpenGlControl? _openGlControl;
    private VulkanControl? _vulkanControl;
    private IRenderer? _renderer;
    private ICamera? _camera;
    private STLModel? _currentModel;
    private RenderSettings _renderSettings = new();
    private ILightingService? _lightingService;

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

        // Check if Vulkan is available, fallback to OpenGL if not
        if (RendererFactory.IsVulkanAvailable())
        {
            UpdateStatusText("Vulkan available - attempting to initialize Vulkan renderer");
            System.Diagnostics.Debug.WriteLine("Vulkan is available, creating Vulkan control");
            Console.WriteLine("🔥 VULKAN DETECTED! Initializing Vulkan renderer...");
            CreateVulkanControl();
        }
        else
        {
            UpdateStatusText("Vulkan not available - falling back to OpenGL");
            System.Diagnostics.Debug.WriteLine("Vulkan not available, falling back to OpenGL");
            Console.WriteLine("⚠️  Vulkan not available, falling back to OpenGL");
            CreateOpenGLControl();
        }

        // Try to get lighting service from service locator (fallback for design-time)
        try
        {
            _lightingService = ServiceLocator.GetService<ILightingService>();
        }
        catch
        {
            // Design-time or DI not available - will use fallback lighting
            _lightingService = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the Viewport3D class with dependency injection.
    /// </summary>
    /// <param name="lightingService">The lighting service.</param>
    public Viewport3D(ILightingService lightingService) : this()
    {
        _lightingService = lightingService;
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

    private void CreateVulkanControl()
    {
        try
        {
            UpdateStatusText("Initializing Vulkan...");
            System.Diagnostics.Debug.WriteLine("CreateVulkanControl: Starting Vulkan initialization");

            // Create the new VulkanControl
            var vulkanControl = new VulkanControl();

            // Set up event handlers
            vulkanControl.StatusChanged += UpdateStatusText;
            vulkanControl.RendererInfoChanged += UpdateRendererText;

            vulkanControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            vulkanControl.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            // Replace the rendering surface
            var renderingSurface = this.FindControl<Border>("RenderingSurface");
            if (renderingSurface != null)
            {
                renderingSurface.Child = vulkanControl;
            }

            // Store reference to the Vulkan control
            _vulkanControl = vulkanControl;

            HideLoadingText();

            System.Diagnostics.Debug.WriteLine("CreateVulkanControl: Vulkan control creation complete");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"CreateVulkanControl: Failed to initialize Vulkan: {ex}");
            UpdateStatusText($"Vulkan initialization failed: {ex.Message}");
            UpdateRendererText("Failed");
            Console.WriteLine($"❌ Vulkan initialization failed: {ex.Message}");

            // Fallback to OpenGL
            System.Diagnostics.Debug.WriteLine("CreateVulkanControl: Falling back to OpenGL");
            Console.WriteLine("🔄 Falling back to OpenGL renderer...");
            CreateOpenGLControl();
        }
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
            UpdateStatusText("Initializing OpenGL...");
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Starting initialization");

            // Create OpenGL wrapper using Avalonia's GL interface
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Creating Silk.NET GL wrapper");
            var silkGl = GL.GetApi(gl.GetProcAddress);
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Silk.NET GL wrapper created successfully");

            // Create renderer
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Creating OpenGL renderer");
            _renderer = RendererFactory.CreateOpenGLRenderer(silkGl);
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: OpenGL renderer created successfully");

            // Create camera
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Creating camera");
            _camera = new Infrastructure.Graphics.Camera();
            _camera.SetPerspective(45.0f, 1.0f, 0.1f, 1000.0f);
            _camera.SetPosition(new Vector3(0, 0, 5));
            _camera.SetTarget(Vector3.Zero);
            _camera.SetUp(Vector3.UnitY);
            System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Camera created successfully");

            // Initialize renderer synchronously on the UI thread
            try
            {
                var width = (int)(_openGlControl?.Bounds.Width ?? 800);
                var height = (int)(_openGlControl?.Bounds.Height ?? 600);
                System.Diagnostics.Debug.WriteLine($"OnOpenGlInit: Initializing renderer with size {width}x{height}");

                var initTask = _renderer.InitializeAsync(width, height);
                initTask.Wait(); // Wait for completion

                System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Renderer initialized successfully");
                _renderer.SetCamera(_camera);
                System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Camera set on renderer");

                var rendererInfo = _renderer.GetInfo();
                System.Diagnostics.Debug.WriteLine($"OnOpenGlInit: Renderer info - {rendererInfo.Name}, API: {rendererInfo.ApiVersion}, Device: {rendererInfo.DeviceName}");

                UpdateStatusText("Renderer initialized successfully");
                UpdateRendererText(rendererInfo.Name);
                HideLoadingText();
                StartRenderLoop();

                System.Diagnostics.Debug.WriteLine("OnOpenGlInit: Initialization complete");
            }
            catch (Exception initEx)
            {
                System.Diagnostics.Debug.WriteLine($"OnOpenGlInit: Renderer initialization failed: {initEx}");
                UpdateStatusText($"Renderer init failed: {initEx.Message}");
                UpdateRendererText("Failed");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnOpenGlInit: Failed to initialize OpenGL: {ex}");
            UpdateStatusText($"Failed to initialize OpenGL: {ex.Message}");
            UpdateRendererText("None");
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

            // Use default render settings (for now)
            // In a complete implementation, these would be synchronized with the ViewModel
            _renderSettings.RenderMode = Core.Interfaces.RenderMode.Surface;
            _renderSettings.Wireframe = false;
            _renderSettings.Lighting.Enabled = true;
            _renderSettings.BackfaceCulling = true;
            _renderSettings.Material = Core.Interfaces.Material.FromPreset(Core.Interfaces.MaterialPreset.Default);
            _renderSettings.EnableTransparency = false;

            // Apply enhanced lighting if lighting service is available
            if (_lightingService != null && _renderSettings.Lighting.Enabled)
            {
                _lightingService.ApplyLightingToRenderSettings(_renderSettings);
            }

            // Clear the screen
            _renderer.Clear(_renderSettings.BackgroundColor);

            // Render the model if available
            if (_currentModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"Rendering model with {_currentModel.TriangleCount} triangles");
                _renderer.Render(_currentModel, _renderSettings);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No model to render");
            }

            // Present the frame
            _renderer.Present();
        }
        catch (Exception ex)
        {
            UpdateStatusText($"Render error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Render error: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Viewport3D: Model loaded with {model.TriangleCount} triangles");

            // Auto-frame the model
            if (_camera != null)
            {
                _camera.FrameToBounds(model.BoundingBox);
                System.Diagnostics.Debug.WriteLine($"Viewport3D: Camera framed to bounds: {model.BoundingBox}");
            }

            // If using Vulkan, load the model into the Vulkan control
            if (_vulkanControl != null)
            {
                // For now, we'll use a placeholder file path
                // In a real implementation, you'd need to store the original file path
                System.Diagnostics.Debug.WriteLine("Viewport3D: Loading model into Vulkan control");
                // _vulkanControl.LoadSTLModel(model.FilePath); // Would need to store file path
            }
        }
        else
        {
            UpdateTriangleCount(0);
            UpdateStatusText("No model loaded");
            System.Diagnostics.Debug.WriteLine("Viewport3D: No model loaded");
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

    private void StartVulkanRenderLoop()
    {
        // Start render timer (60 FPS target)
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60.0)
        };
        _renderTimer.Tick += (_, _) => VulkanRender();
        _renderTimer.Start();

        // Start FPS timer
        _fpsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _fpsTimer.Tick += (_, _) => UpdateFpsDisplay();
        _fpsTimer.Start();
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

    private void VulkanRender()
    {
        if (_renderer == null || _camera == null)
            return;

        _frameTimer.Restart();

        try
        {
            // Use default render settings
            _renderSettings.RenderMode = Core.Interfaces.RenderMode.Surface;
            _renderSettings.Wireframe = false;
            _renderSettings.Lighting.Enabled = true;
            _renderSettings.BackfaceCulling = true;
            _renderSettings.Material = Core.Interfaces.Material.FromPreset(Core.Interfaces.MaterialPreset.Default);
            _renderSettings.EnableTransparency = false;

            // Apply enhanced lighting if lighting service is available
            if (_lightingService != null && _renderSettings.Lighting.Enabled)
            {
                _lightingService.ApplyLightingToRenderSettings(_renderSettings);
            }

            // Clear the screen
            _renderer.Clear(_renderSettings.BackgroundColor);

            // Render the model if available
            if (_currentModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"Vulkan rendering model with {_currentModel.TriangleCount} triangles");
                _renderer.Render(_currentModel, _renderSettings);
            }

            // Present the frame
            _renderer.Present();
        }
        catch (Exception ex)
        {
            UpdateStatusText($"Vulkan render error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Vulkan render error: {ex.Message}");
        }

        _frameTimer.Stop();
        UpdateFrameTime(_frameTimer.Elapsed.TotalMilliseconds);
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
        {
            rendererText.Text = $"Renderer: {text}";
            System.Diagnostics.Debug.WriteLine($"UpdateRendererText: {text}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("UpdateRendererText: RendererText control not found");
        }
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


