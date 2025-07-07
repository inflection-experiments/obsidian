# STL Viewer Desktop Application - Project Requirements Document

## 1. Project Overview

### 1.1 Executive Summary
A cross-platform desktop application for viewing and manipulating STL files with advanced 3D visualization capabilities, featuring a fighter plane model with flight path visualization. The application leverages modern C# features with Avalonia UI framework following MVVM architecture pattern.

### 1.2 Key Objectives
- Provide intuitive STL file loading and 3D manipulation
- Support multiple rendering modes (surface, wireframe, shaded wireframe)
- Implement flight path visualization for fighter plane models
- Ensure high performance through multi-threaded architecture
- Maintain clean, modern UI with consistent theming
- Support multiple languages through localization
- Integrate with backend APIs for extended functionality

## 2. Technical Stack

### 2.1 Core Technologies
- **Language**: C# 12.0 with .NET 8.0
- **UI Framework**: Avalonia 11.0+
- **3D Graphics**: 
  - Silk.NET (OpenGL bindings)
  - HelixToolkit.Avalonia for 3D controls
- **MVVM Framework**: ReactiveUI 19.0+
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **API Communication**: Refit 7.0+ with Polly for resilience
- **Localization**: ResX Resource Manager
- **Testing**: 
  - xUnit for unit tests
  - FluentAssertions for assertions
  - Moq for mocking
  - Avalonia.Headless for UI testing
- **Logging**: Serilog with structured logging
- **Validation**: FluentValidation

### 2.2 Additional Libraries
- **MathNET.Numerics**: Mathematical computations
- **SkiaSharp**: 2D graphics and charting
- **OxyPlot.Avalonia**: Advanced plotting
- **MessagePack**: Binary serialization
- **System.Reactive**: Reactive extensions

## 3. Architecture Overview

### 3.1 Solution Structure
```
STLViewer/
├── src/
│   ├── STLViewer.Core/
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   └── Extensions/
│   ├── STLViewer.Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   └── Events/
│   ├── STLViewer.Infrastructure/
│   │   ├── API/
│   │   ├── FileSystem/
│   │   ├── Graphics/
│   │   └── Persistence/
│   ├── STLViewer.Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── Validators/
│   │   └── Mapping/
│   ├── STLViewer.UI/
│   │   ├── Views/
│   │   ├── ViewModels/
│   │   ├── Controls/
│   │   ├── Converters/
│   │   ├── Behaviors/
│   │   └── Resources/
│   └── STLViewer.Desktop/
│       └── Program.cs
├── tests/
│   ├── STLViewer.Core.Tests/
│   ├── STLViewer.Application.Tests/
│   ├── STLViewer.Infrastructure.Tests/
│   └── STLViewer.UI.Tests/
├── resources/
│   ├── Models/
│   │   └── fighter-plane.stl
│   └── Localization/
└── docs/
```

### 3.2 Layer Responsibilities
- **Core**: Business logic, domain models, interfaces
- **Domain**: Domain entities, value objects, domain events
- **Infrastructure**: External service implementations, file I/O, graphics rendering
- **Application**: Use cases, application services, DTOs
- **UI**: Views, ViewModels, UI-specific logic
- **Desktop**: Application entry point, bootstrapping

## 4. Core Features

### 4.1 STL File Management
- File loading with drag-and-drop support
- Recent files tracking
- File validation and error handling
- Support for ASCII and binary STL formats
- Batch file loading

### 4.2 3D Visualization
- **Rendering Modes**:
  - Surface rendering with material properties
  - Wireframe mode
  - Shaded wireframe (hybrid)
- **Camera Controls**:
  - Orbit rotation
  - Pan translation
  - Zoom (scroll wheel)
  - Predefined views (front, top, side, isometric)
- **Lighting**:
  - Ambient lighting
  - Directional lights
  - Point lights with adjustable properties
- **Materials**:
  - Color selection with color picker
  - Material presets (metal, plastic, matte)
  - Transparency control

### 4.3 Fighter Plane Visualization
- Pre-loaded fighter plane model
- Flight path definition and editing
- Waypoint management
- Speed and trajectory control
- Real-time flight simulation
- Trail visualization
- Cockpit view mode

### 4.4 Scene Management
- Multiple STL files in single scene
- Layer management
- Object hierarchy
- Transform gizmos
- Snap-to-grid functionality

### 4.5 Measurement Tools
- Distance measurement
- Angle measurement
- Volume calculation
- Surface area calculation
- Bounding box display

### 4.6 Data Visualization
- Performance metrics graphs
- Flight telemetry charts
- Real-time FPS counter
- Memory usage monitoring

## 5. Detailed Implementation Steps

### 5.1 Project Setup and Infrastructure

#### Step 1: Create Solution Structure
```csharp
// Create solution file
dotnet new sln -n STLViewer

// Create projects
dotnet new classlib -n STLViewer.Core -f net8.0
dotnet new classlib -n STLViewer.Domain -f net8.0
dotnet new classlib -n STLViewer.Infrastructure -f net8.0
dotnet new classlib -n STLViewer.Application -f net8.0
dotnet new avalonia.mvvm -n STLViewer.UI -f net8.0
dotnet new avalonia -n STLViewer.Desktop -f net8.0

// Add projects to solution
dotnet sln add src/**/*.csproj
```

#### Step 2: Configure NuGet Packages
```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### 5.2 Domain Layer Implementation

#### Step 3: Define Core Domain Models
```csharp
// STLViewer.Domain/Entities/STLModel.cs
public sealed class STLModel : Entity<Guid>
{
    public string FileName { get; private set; }
    public byte[] Data { get; private set; }
    public STLFormat Format { get; private set; }
    public BoundingBox Bounds { get; private set; }
    public IReadOnlyList<Triangle> Triangles { get; private set; }
    public ModelMetadata Metadata { get; private set; }
    
    private STLModel() { } // EF Core
    
    public static Result<STLModel> Create(
        string fileName, 
        byte[] data,
        ISTLParser parser)
    {
        // Validation and parsing logic
    }
}

// STLViewer.Domain/ValueObjects/Triangle.cs
public readonly record struct Triangle(
    Vector3 Vertex1,
    Vector3 Vertex2,
    Vector3 Vertex3,
    Vector3 Normal)
{
    public float Area => CalculateArea();
    
    private float CalculateArea()
    {
        var edge1 = Vertex2 - Vertex1;
        var edge2 = Vertex3 - Vertex1;
        return 0.5f * Vector3.Cross(edge1, edge2).Length();
    }
}
```

### 5.3 Infrastructure Layer

#### Step 4: Implement STL Parser
```csharp
// STLViewer.Infrastructure/FileSystem/STLParser.cs
public sealed class STLParser : ISTLParser
{
    private readonly ILogger<STLParser> _logger;
    
    public async Task<Result<ParsedSTL>> ParseAsync(
        Stream stream, 
        CancellationToken cancellationToken = default)
    {
        var format = await DetectFormatAsync(stream, cancellationToken);
        
        return format switch
        {
            STLFormat.ASCII => await ParseAsciiAsync(stream, cancellationToken),
            STLFormat.Binary => await ParseBinaryAsync(stream, cancellationToken),
            _ => Result<ParsedSTL>.Failure("Unknown STL format")
        };
    }
    
    private async Task<Result<ParsedSTL>> ParseBinaryAsync(
        Stream stream, 
        CancellationToken cancellationToken)
    {
        using var reader = new BinaryReader(stream);
        
        // Skip 80-byte header
        reader.ReadBytes(80);
        
        var triangleCount = reader.ReadUInt32();
        var triangles = new List<Triangle>((int)triangleCount);
        
        var progress = new Progress<float>();
        
        await Task.Run(() =>
        {
            for (int i = 0; i < triangleCount; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var normal = ReadVector3(reader);
                var v1 = ReadVector3(reader);
                var v2 = ReadVector3(reader);
                var v3 = ReadVector3(reader);
                reader.ReadUInt16(); // Attribute byte count
                
                triangles.Add(new Triangle(v1, v2, v3, normal));
                
                if (i % 1000 == 0)
                    progress.Report((float)i / triangleCount);
            }
        }, cancellationToken);
        
        return Result<ParsedSTL>.Success(new ParsedSTL(triangles));
    }
}
```

#### Step 5: Implement 3D Graphics Renderer
```csharp
// STLViewer.Infrastructure/Graphics/OpenGLRenderer.cs
public sealed class OpenGLRenderer : IRenderer
{
    private readonly ILogger<OpenGLRenderer> _logger;
    private GL _gl;
    private uint _vao, _vbo, _ebo;
    private uint _shaderProgram;
    private readonly ConcurrentQueue<RenderCommand> _renderQueue;
    
    public async Task InitializeAsync(nint windowHandle)
    {
        _gl = GL.GetApi(windowHandle);
        
        // Initialize OpenGL context
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        
        // Compile shaders
        _shaderProgram = await CompileShadersAsync();
        
        // Setup vertex arrays
        SetupVertexArrays();
    }
    
    public void Render(Scene scene, Camera camera)
    {
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        _gl.UseProgram(_shaderProgram);
        
        // Set uniforms
        var viewMatrix = camera.GetViewMatrix();
        var projectionMatrix = camera.GetProjectionMatrix();
        
        _gl.UniformMatrix4(_gl.GetUniformLocation(_shaderProgram, "view"), 
            1, false, viewMatrix);
        _gl.UniformMatrix4(_gl.GetUniformLocation(_shaderProgram, "projection"), 
            1, false, projectionMatrix);
        
        // Process render queue
        while (_renderQueue.TryDequeue(out var command))
        {
            ExecuteRenderCommand(command);
        }
        
        // Render scene objects
        Parallel.ForEach(scene.RenderableObjects, renderObject =>
        {
            RenderObject(renderObject, camera);
        });
    }
}
```

### 5.4 Application Layer

#### Step 6: Implement Commands and Queries
```csharp
// STLViewer.Application/Commands/LoadSTLFileCommand.cs
public sealed record LoadSTLFileCommand(string FilePath) : IRequest<Result<STLModelDto>>;

public sealed class LoadSTLFileCommandHandler : IRequestHandler<LoadSTLFileCommand, Result<STLModelDto>>
{
    private readonly ISTLParser _parser;
    private readonly ISTLRepository _repository;
    private readonly IMapper _mapper;
    
    public async Task<Result<STLModelDto>> Handle(
        LoadSTLFileCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(request.FilePath);
            var parseResult = await _parser.ParseAsync(stream, cancellationToken);
            
            if (parseResult.IsFailure)
                return Result<STLModelDto>.Failure(parseResult.Error);
            
            var model = STLModel.Create(
                Path.GetFileName(request.FilePath),
                await File.ReadAllBytesAsync(request.FilePath, cancellationToken),
                _parser);
            
            await _repository.AddAsync(model.Value, cancellationToken);
            
            return Result<STLModelDto>.Success(_mapper.Map<STLModelDto>(model.Value));
        }
        catch (Exception ex)
        {
            return Result<STLModelDto>.Failure($"Failed to load STL file: {ex.Message}");
        }
    }
}
```

### 5.5 UI Layer - MVVM Implementation

#### Step 7: Create ViewModels
```csharp
// STLViewer.UI/ViewModels/MainViewModel.cs
public sealed class MainViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainViewModel> _logger;
    
    private STLModelDto? _currentModel;
    private RenderMode _renderMode = RenderMode.Surface;
    private Color _selectedColor = Colors.Gray;
    
    public MainViewModel(
        IMediator mediator,
        IDialogService dialogService,
        ILogger<MainViewModel> logger)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        _logger = logger;
        
        InitializeCommands();
        InitializeSubscriptions();
    }
    
    public STLModelDto? CurrentModel
    {
        get => _currentModel;
        set => this.RaiseAndSetIfChanged(ref _currentModel, value);
    }
    
    public ReactiveCommand<Unit, Unit> LoadFileCommand { get; private set; }
    public ReactiveCommand<Unit, Unit> ToggleRenderModeCommand { get; private set; }
    
    private void InitializeCommands()
    {
        LoadFileCommand = ReactiveCommand.CreateFromTask(LoadFileAsync);
        
        ToggleRenderModeCommand = ReactiveCommand.Create(() =>
        {
            RenderMode = RenderMode switch
            {
                RenderMode.Surface => RenderMode.Wireframe,
                RenderMode.Wireframe => RenderMode.WireframeShaded,
                RenderMode.WireframeShaded => RenderMode.Surface,
                _ => RenderMode.Surface
            };
        });
    }
    
    private async Task LoadFileAsync()
    {
        var files = await _dialogService.ShowOpenFileDialogAsync(
            new OpenFileDialogSettings
            {
                Title = "Select STL File",
                Filters = new[] { new FileFilter("STL Files", "*.stl") }
            });
        
        if (files?.Any() == true)
        {
            var result = await _mediator.Send(new LoadSTLFileCommand(files.First()));
            
            if (result.IsSuccess)
            {
                CurrentModel = result.Value;
            }
            else
            {
                await _dialogService.ShowErrorAsync("Load Error", result.Error);
            }
        }
    }
}
```

#### Step 8: Create Views
```xml
<!-- STLViewer.UI/Views/MainView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:STLViewer.UI.ViewModels"
             xmlns:controls="using:STLViewer.UI.Controls"
             x:Class="STLViewer.UI.Views.MainView"
             x:DataType="vm:MainViewModel">
    
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Toolbar -->
        <Border Grid.Row="0" Classes="toolbar">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Button Command="{Binding LoadFileCommand}"
                        ToolTip.Tip="{x:Static localization:Resources.LoadFile}">
                    <PathIcon Data="{StaticResource OpenFileIcon}"/>
                </Button>
                
                <Separator/>
                
                <ToggleButton IsChecked="{Binding IsSurfaceMode}"
                              ToolTip.Tip="{x:Static localization:Resources.SurfaceMode}">
                    <PathIcon Data="{StaticResource SurfaceIcon}"/>
                </ToggleButton>
                
                <ToggleButton IsChecked="{Binding IsWireframeMode}"
                              ToolTip.Tip="{x:Static localization:Resources.WireframeMode}">
                    <PathIcon Data="{StaticResource WireframeIcon}"/>
                </ToggleButton>
                
                <ColorPicker Color="{Binding SelectedColor}"/>
            </StackPanel>
        </Border>
        
        <!-- 3D Viewport -->
        <controls:Viewport3D Grid.Row="1"
                            Model="{Binding CurrentModel}"
                            RenderMode="{Binding RenderMode}"
                            ModelColor="{Binding SelectedColor}">
            <controls:Viewport3D.ContextMenu>
                <ContextMenu>
                    <MenuItem Header="{x:Static localization:Resources.ResetView}"
                             Command="{Binding ResetViewCommand}"/>
                    <Separator/>
                    <MenuItem Header="{x:Static localization:Resources.ViewFront}"
                             Command="{Binding SetFrontViewCommand}"/>
                    <MenuItem Header="{x:Static localization:Resources.ViewTop}"
                             Command="{Binding SetTopViewCommand}"/>
                </ContextMenu>
            </controls:Viewport3D.ContextMenu>
        </controls:Viewport3D>
        
        <!-- Status Bar -->
        <Border Grid.Row="2" Classes="statusbar">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <TextBlock Grid.Column="0" 
                          Text="{Binding CurrentModel.FileName, 
                                 FallbackValue={x:Static localization:Resources.NoFileLoaded}}"/>
                
                <TextBlock Grid.Column="1" 
                          Text="{Binding CurrentModel.TriangleCount, 
                                 StringFormat='Triangles: {0:N0}'}"
                          Margin="16,0"/>
                
                <TextBlock Grid.Column="2" 
                          Text="{Binding FPS, StringFormat='FPS: {0:F1}'}"/>
            </Grid>
        </Border>
    </Grid>
</UserControl>
```

### 5.6 3D Viewport Control Implementation

#### Step 9: Create Custom 3D Control
```csharp
// STLViewer.UI/Controls/Viewport3D.cs
public class Viewport3D : NativeControlHost
{
    private IRenderer? _renderer;
    private Camera _camera;
    private readonly Subject<PointerEventArgs> _pointerEvents;
    
    public static readonly StyledProperty<STLModelDto?> ModelProperty =
        AvaloniaProperty.Register<Viewport3D, STLModelDto?>(nameof(Model));
    
    public static readonly StyledProperty<RenderMode> RenderModeProperty =
        AvaloniaProperty.Register<Viewport3D, RenderMode>(
            nameof(RenderMode), 
            RenderMode.Surface);
    
    public STLModelDto? Model
    {
        get => GetValue(ModelProperty);
        set => SetValue(ModelProperty, value);
    }
    
    protected override IPlatformHandle CreateNativeControlCore(
        IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);
        
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            _renderer = new OpenGLRenderer();
            await _renderer.InitializeAsync(handle.Handle);
            
            StartRenderLoop();
        });
        
        return handle;
    }
    
    private void StartRenderLoop()
    {
        var renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // 60 FPS
        };
        
        renderTimer.Tick += (s, e) =>
        {
            if (_renderer != null && Model != null)
            {
                var scene = BuildScene();
                _renderer.Render(scene, _camera);
            }
        };
        
        renderTimer.Start();
    }
    
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _lastPointerPosition = e.GetPosition(this);
        e.Handled = true;
    }
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _lastPointerPosition;
            
            _camera.Rotate(delta.X * 0.01f, delta.Y * 0.01f);
            _lastPointerPosition = currentPosition;
        }
    }
}
```

### 5.7 Theme and Styling

#### Step 10: Implement Design System
```xml
<!-- STLViewer.UI/Resources/Themes/Default.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Color Palette -->
    <Color x:Key="PrimaryColor">#2196F3</Color>
    <Color x:Key="SecondaryColor">#FFC107</Color>
    <Color x:Key="BackgroundColor">#1E1E1E</Color>
    <Color x:Key="SurfaceColor">#2D2D2D</Color>
    <Color x:Key="OnSurfaceColor">#FFFFFF</Color>
    
    <!-- Typography -->
    <FontFamily x:Key="DefaultFontFamily">Segoe UI, San Francisco, Ubuntu</FontFamily>
    
    <!-- Control Styles -->
    <Style Selector="Button">
        <Setter Property="Background" Value="{DynamicResource PrimaryColor}"/>
        <Setter Property="Foreground" Value="{DynamicResource OnSurfaceColor}"/>
        <Setter Property="Padding" Value="16,8"/>
        <Setter Property="CornerRadius" Value="4"/>
        <Setter Property="Transitions">
            <Transitions>
                <DoubleTransition Property="Opacity" Duration="0:0:0.2"/>
            </Transitions>
        </Setter>
    </Style>
    
    <Style Selector="Button:pointerover">
        <Setter Property="Opacity" Value="0.8"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>
    
    <!-- Custom Control Templates -->
    <ControlTheme x:Key="ModernWindow" TargetType="Window">
        <Setter Property="Background" Value="{DynamicResource BackgroundColor}"/>
        <Setter Property="TransparencyLevelHint" Value="AcrylicBlur"/>
        <Setter Property="ExtendClientAreaToDecorationsHint" Value="True"/>
    </ControlTheme>
</ResourceDictionary>
```

### 5.8 Backend API Integration

#### Step 11: Create API Client
```csharp
// STLViewer.Infrastructure/API/ISTLViewerApi.cs
public interface ISTLViewerApi
{
    [Post("/api/models/analyze")]
    Task<ModelAnalysisResult> AnalyzeModelAsync([Body] byte[] modelData);
    
    [Get("/api/models/library")]
    Task<IEnumerable<ModelMetadata>> GetModelLibraryAsync();
    
    [Post("/api/flightpath/optimize")]
    Task<OptimizedFlightPath> OptimizeFlightPathAsync([Body] FlightPathRequest request);
}

// STLViewer.Infrastructure/API/ApiClient.cs
public sealed class ApiClient : IApiClient
{
    private readonly ISTLViewerApi _api;
    private readonly ILogger<ApiClient> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    
    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _logger = logger;
        
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "API request failed. Retry {RetryCount} after {Timespan}s",
                        retryCount, timespan.TotalSeconds);
                });
        
        _api = RestService.For<ISTLViewerApi>(httpClient, new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
        });
    }
}
```

### 5.9 Localization Support

#### Step 12: Implement Localization
```csharp
// STLViewer.UI/Localization/LocalizationService.cs
public sealed class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private CultureInfo _currentCulture;
    
    public LocalizationService()
    {
        _resourceManager = new ResourceManager(
            "STLViewer.UI.Resources.Strings",
            typeof(LocalizationService).Assembly);
        
        _currentCulture = CultureInfo.CurrentUICulture;
    }
    
    public string GetString(string key)
    {
        return _resourceManager.GetString(key, _currentCulture) ?? key;
    }
    
    public void SetCulture(string cultureName)
    {
        _currentCulture = new CultureInfo(cultureName);
        CultureInfo.CurrentUICulture = _currentCulture;
        
        // Raise culture changed event
        CultureChanged?.Invoke(this, _currentCulture);
    }
    
    public event EventHandler<CultureInfo>? CultureChanged;
}

// Create markup extension for XAML
public class LocalizeExtension : MarkupExtension
{
    public string Key { get; set; }
    
    public LocalizeExtension(string key)
    {
        Key = key;
    }
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var localizationService = App.Current.Services
            .GetRequiredService<ILocalizationService>();
            
        return localizationService.GetString(Key);
    }
}
```

### 5.10 Fighter Plane Scene Management

#### Step 13: Implement Flight Path System
```csharp
// STLViewer.Core/Services/FlightPathService.cs
public sealed class FlightPathService : IFlightPathService
{
    private readonly ILogger<FlightPathService> _logger;
    private readonly Subject<FlightTelemetry> _telemetryStream;
    
    public IObservable<FlightTelemetry> TelemetryStream => _telemetryStream;
    
    public async Task<FlightPath> CreateFlightPathAsync(
        IEnumerable<Waypoint> waypoints,
        FlightParameters parameters)
    {
        var path = new FlightPath(waypoints.ToList(), parameters);
        
        // Calculate spline interpolation for smooth path
        var spline = await Task.Run(() => 
            CubicSpline.InterpolateNatural(
                waypoints.Select(w => w.Time).ToArray(),
                waypoints.Select(w => w.Position).ToArray()));
        
        path.SetInterpolation(spline);
        
        return path;
    }
    
    public async Task SimulateFlightAsync(
        FlightPath path,
        STLModel fighterModel,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var physicsEngine = new FlightPhysicsEngine();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            var position = path.GetPositionAtTime(elapsed);
            var rotation = path.GetRotationAtTime(elapsed);
            
            var telemetry = new FlightTelemetry
            {
                Position = position,
                Rotation = rotation,
                Speed = physicsEngine.CalculateSpeed(position, elapsed),
                Altitude = position.Y,
                GForce = physicsEngine.CalculateGForce(position, elapsed),
                Timestamp = DateTime.UtcNow
            };
            
            _telemetryStream.OnNext(telemetry);
            
            await Task.Delay(16, cancellationToken); // 60 FPS
        }
    }
}
```

### 5.11 Multi-threading Architecture

#### Step 14: Implement Thread-Safe Operations
```csharp
// STLViewer.Core/Threading/RenderThreadManager.cs
public sealed class RenderThreadManager : IRenderThreadManager
{
    private readonly BlockingCollection<RenderTask> _renderQueue;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Thread _renderThread;
    private readonly ILogger<RenderThreadManager> _logger;
    
    public RenderThreadManager(ILogger<RenderThreadManager> logger)
    {
        _logger = logger;
        _renderQueue = new BlockingCollection<RenderTask>(1000);
        _cancellationTokenSource = new CancellationTokenSource();
        
        _renderThread = new Thread(RenderThreadProc)
        {
            Name = "RenderThread",
            IsBackground = false,
            Priority = ThreadPriority.AboveNormal
        };
        
        _renderThread.Start();
    }
    
    public void QueueRenderTask(RenderTask task)
    {
        if (!_renderQueue.TryAdd(task))
        {
            _logger.LogWarning("Render queue full, dropping frame");
        }
    }
    
    private void RenderThreadProc()
    {
        try
        {
            foreach (var task in _renderQueue.GetConsumingEnumerable(
                _cancellationTokenSource.Token))
            {
                try
                {
                    task.Execute();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing render task");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Render thread shutting down");
        }
    }
}

// Background processing for file loading
public sealed class FileLoadingService : IFileLoadingService
{
    private readonly Channel<FileLoadRequest> _loadQueue;
    private readonly ISTLParser _parser;
    private readonly SemaphoreSlim _semaphore;
    
    public FileLoadingService(ISTLParser parser)
    {
        _parser = parser;
        _loadQueue = Channel.CreateUnbounded<FileLoadRequest>();
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        
        _ = ProcessLoadQueueAsync();
    }
    
    private async Task ProcessLoadQueueAsync()
    {
        await foreach (var request in _loadQueue.Reader.ReadAllAsync())
        {
            await _semaphore.WaitAsync();
            
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await LoadFileInternalAsync(request);
                    request.CompletionSource.SetResult(result);
                }
                finally
                {
                    _semaphore.Release();
                }
            });
        }
    }
}
```

### 5.12 Testing Framework

#### Step 15: Implement Comprehensive Tests
```csharp
// STLViewer.Core.Tests/Services/STLParserTests.cs
public class STLParserTests
{
    private readonly ITestOutputHelper _output;
    private readonly STLParser _parser;
    
    public STLParserTests(ITestOutputHelper output)
    {
        _output = output;
        _parser = new STLParser(new NullLogger<STLParser>());
    }
    
    [Fact]
    public async Task ParseAsync_ValidBinarySTL_ReturnsSuccess()
    {
        // Arrange
        var stlData = GenerateBinarySTL(100);
        using var stream = new MemoryStream(stlData);
        
        // Act
        var result = await _parser.ParseAsync(stream);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Triangles.Should().HaveCount(100);
    }
    
    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task ParseAsync_Performance_MeetsRequirements(int triangleCount)
    {
        // Arrange
        var stlData = GenerateBinarySTL(triangleCount);
        using var stream = new MemoryStream(stlData);
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var result = await _parser.ParseAsync(stream);
        stopwatch.Stop();
        
        // Assert
        _output.WriteLine($"Parsed {triangleCount} triangles in {stopwatch.ElapsedMilliseconds}ms");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(triangleCount); // 1ms per triangle max
    }
}

// UI Testing
public class MainViewModelTests
{
    private readonly MainViewModel _viewModel;
    private readonly Mock<IMediator> _mediatorMock;
    
    public MainViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        var dialogServiceMock = new Mock<IDialogService>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();
        
        _viewModel = new MainViewModel(
            _mediatorMock.Object,
            dialogServiceMock.Object,
            loggerMock.Object);
    }
    
    [Fact]
    public async Task LoadFileCommand_ValidFile_UpdatesCurrentModel()
    {
        // Arrange
        var expectedModel = new STLModelDto { FileName = "test.stl" };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<LoadSTLFileCommand>(), default))
            .ReturnsAsync(Result<STLModelDto>.Success(expectedModel));
        
        // Act
        await _viewModel.LoadFileCommand.Execute();
        
        // Assert
        _viewModel.CurrentModel.Should().Be(expectedModel);
    }
}
```

### 5.13 Visualization Components

#### Step 16: Implement Charts and Graphs
```csharp
// STLViewer.UI/Controls/TelemetryChart.cs
public class TelemetryChart : UserControl
{
    private readonly PlotModel _plotModel;
    private readonly LineSeries _speedSeries;
    private readonly LineSeries _altitudeSeries;
    private readonly LineSeries _gForceSeries;
    
    public TelemetryChart()
    {
        _plotModel = new PlotModel
        {
            Title = "Flight Telemetry",
            Background = OxyColors.Transparent,
            PlotAreaBorderColor = OxyColors.Gray
        };
        
        _speedSeries = new LineSeries
        {
            Title = "Speed (m/s)",
            Color = OxyColors.Blue
        };
        
        _altitudeSeries = new LineSeries
        {
            Title = "Altitude (m)",
            Color = OxyColors.Green,
            YAxisKey = "altitude"
        };
        
        _gForceSeries = new LineSeries
        {
            Title = "G-Force",
            Color = OxyColors.Red,
            YAxisKey = "gforce"
        };
        
        SetupAxes();
        
        Content = new PlotView { Model = _plotModel };
    }
    
    public void AddDataPoint(FlightTelemetry telemetry)
    {
        var time = (telemetry.Timestamp - _startTime).TotalSeconds;
        
        _speedSeries.Points.Add(new DataPoint(time, telemetry.Speed));
        _altitudeSeries.Points.Add(new DataPoint(time, telemetry.Altitude));
        _gForceSeries.Points.Add(new DataPoint(time, telemetry.GForce));
        
        // Keep only last 1000 points
        if (_speedSeries.Points.Count > 1000)
        {
            _speedSeries.Points.RemoveAt(0);
            _altitudeSeries.Points.RemoveAt(0);
            _gForceSeries.Points.RemoveAt(0);
        }
        
        _plotModel.InvalidatePlot(true);
    }
}
```

### 5.14 Application Bootstrap

#### Step 17: Configure Dependency Injection
```csharp
// STLViewer.Desktop/Program.cs
public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
    
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .ConfigureServices(ConfigureServices);
    
    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/stlviewer-.log", 
                    rollingInterval: RollingInterval.Day)
                .WriteTo.Console()
                .CreateLogger());
        });
        
        // Core Services
        services.AddSingleton<ISTLParser, STLParser>();
        services.AddSingleton<IRenderer, OpenGLRenderer>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IRenderThreadManager, RenderThreadManager>();
        
        // MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(LoadSTLFileCommand).Assembly);
            cfg.AddBehavior<IPipelineBehavior<,>, ValidationBehavior<,>>();
            cfg.AddBehavior<IPipelineBehavior<,>, LoggingBehavior<,>>();
        });
        
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(LoadSTLFileCommandValidator).Assembly);
        
        // HTTP Client
        services.AddHttpClient<IApiClient, ApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.stlviewer.com");
            client.DefaultRequestHeaders.Add("User-Agent", "STLViewer/1.0");
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
        
        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<FlightControlViewModel>();
        
        // Background Services
        services.AddHostedService<TelemetryCollectorService>();
        services.AddHostedService<CacheCleanupService>();
    }
}
```

## 6. Testing Strategy

### 6.1 Unit Testing
- Test coverage target: 80%
- Mock external dependencies
- Test business logic isolation
- Performance benchmarks

### 6.2 Integration Testing
- Test API integration
- File system operations
- Database operations
- Multi-threaded scenarios

### 6.3 UI Testing
- Avalonia.Headless for UI tests
- Screenshot comparison
- User interaction simulation
- Performance profiling

### 6.4 End-to-End Testing
- Complete user workflows
- Cross-platform testing
- Localization testing
- Load testing

## 7. Best Practices Implementation

### 7.1 Code Quality
- Nullable reference types enabled
- Code analysis rules (StyleCop, FxCop)
- EditorConfig for consistency
- Pre-commit hooks

### 7.2 Architecture Patterns
- Clean Architecture principles
- SOLID principles
- DDD tactical patterns
- CQRS for complex operations

### 7.3 Performance Optimization
- Async/await throughout
- Object pooling for frequent allocations
- Span<T> for buffer operations
- SIMD operations for math

### 7.4 Security
- Input validation
- Secure API communication
- File upload restrictions
- Memory safety

### 7.5 Documentation
- XML documentation for public APIs
- Architecture Decision Records (ADRs)
- User manual
- Developer guide

## 8. Deployment Considerations

### 8.1 Build Pipeline
- Multi-platform builds
- Automated versioning
- Code signing
- Package creation

### 8.2 Distribution
- Windows: MSI installer
- macOS: DMG package
- Linux: AppImage/Snap
- Auto-update mechanism

### 8.3 Monitoring
- Application insights
- Crash reporting
- Performance metrics
- Usage analytics

This comprehensive requirements document provides a solid foundation for building a professional-grade STL viewer application with modern architecture and best practices.