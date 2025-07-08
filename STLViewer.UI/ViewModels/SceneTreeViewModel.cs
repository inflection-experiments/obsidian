using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using MediatR;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Application.Commands.Scene;
using STLViewer.Application.Queries.Scene;

namespace STLViewer.UI.ViewModels;

/// <summary>
/// ViewModel for managing the scene hierarchy tree.
/// </summary>
public class SceneTreeViewModel : ViewModelBase
{
    private readonly ISceneManager _sceneManager;
    private readonly IMediator _mediator;
    private Scene? _currentScene;
    private SceneNodeViewModel? _selectedNode;
    private LayerViewModel? _selectedLayer;

    /// <summary>
    /// Initializes a new instance of the SceneTreeViewModel class.
    /// </summary>
    /// <param name="sceneManager">The scene manager.</param>
    /// <param name="mediator">The mediator.</param>
    public SceneTreeViewModel(ISceneManager sceneManager, IMediator mediator)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        // Initialize collections
        RootNodes = new ObservableCollection<SceneNodeViewModel>();
        Layers = new ObservableCollection<LayerViewModel>();

        // Initialize commands
        CreateSceneCommand = ReactiveCommand.CreateFromTask<string>(CreateSceneAsync);
        CreateLayerCommand = ReactiveCommand.CreateFromTask<string>(CreateLayerAsync);
        CreateGroupCommand = ReactiveCommand.CreateFromTask<string>(CreateGroupAsync);
        DeleteNodeCommand = ReactiveCommand.CreateFromTask<Guid>(DeleteNodeAsync);
        DeleteLayerCommand = ReactiveCommand.CreateFromTask<Guid>(DeleteLayerAsync);
        MoveToLayerCommand = ReactiveCommand.CreateFromTask<(Guid nodeId, Guid layerId)>(MoveToLayerAsync);
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);

        // Subscribe to scene changes
        _sceneManager.CurrentSceneChanged += OnCurrentSceneChanged;
        _sceneManager.SceneModified += OnSceneModified;

        // Load current scene
        LoadCurrentScene();
    }

    /// <summary>
    /// Gets the collection of root nodes in the scene.
    /// </summary>
    public ObservableCollection<SceneNodeViewModel> RootNodes { get; }

    /// <summary>
    /// Gets the collection of layers in the scene.
    /// </summary>
    public ObservableCollection<LayerViewModel> Layers { get; }

    /// <summary>
    /// Gets or sets the currently selected node.
    /// </summary>
    public SceneNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set => this.RaiseAndSetIfChanged(ref _selectedNode, value);
    }

    /// <summary>
    /// Gets or sets the currently selected layer.
    /// </summary>
    public LayerViewModel? SelectedLayer
    {
        get => _selectedLayer;
        set => this.RaiseAndSetIfChanged(ref _selectedLayer, value);
    }

    /// <summary>
    /// Gets the current scene.
    /// </summary>
    public Scene? CurrentScene
    {
        get => _currentScene;
        private set => this.RaiseAndSetIfChanged(ref _currentScene, value);
    }

    /// <summary>
    /// Gets the command to create a new scene.
    /// </summary>
    public ReactiveCommand<string, System.Reactive.Unit> CreateSceneCommand { get; }

    /// <summary>
    /// Gets the command to create a new layer.
    /// </summary>
    public ReactiveCommand<string, System.Reactive.Unit> CreateLayerCommand { get; }

    /// <summary>
    /// Gets the command to create a new group.
    /// </summary>
    public ReactiveCommand<string, System.Reactive.Unit> CreateGroupCommand { get; }

    /// <summary>
    /// Gets the command to delete a node.
    /// </summary>
    public ReactiveCommand<Guid, System.Reactive.Unit> DeleteNodeCommand { get; }

    /// <summary>
    /// Gets the command to delete a layer.
    /// </summary>
    public ReactiveCommand<Guid, System.Reactive.Unit> DeleteLayerCommand { get; }

    /// <summary>
    /// Gets the command to move a node to a different layer.
    /// </summary>
    public ReactiveCommand<(Guid nodeId, Guid layerId), System.Reactive.Unit> MoveToLayerCommand { get; }

    /// <summary>
    /// Gets the command to refresh the scene tree.
    /// </summary>
    public ReactiveCommand<System.Reactive.Unit, System.Reactive.Unit> RefreshCommand { get; }

    /// <summary>
    /// Gets scene statistics.
    /// </summary>
    public async Task<SceneStatistics?> GetSceneStatisticsAsync()
    {
        var result = await _mediator.Send(new GetSceneStatisticsQuery());
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// Expands a node in the tree.
    /// </summary>
    /// <param name="nodeId">The ID of the node to expand.</param>
    public void ExpandNode(Guid nodeId)
    {
        var node = FindNodeViewModel(nodeId);
        if (node != null)
        {
            node.IsExpanded = true;
        }
    }

    /// <summary>
    /// Collapses a node in the tree.
    /// </summary>
    /// <param name="nodeId">The ID of the node to collapse.</param>
    public void CollapseNode(Guid nodeId)
    {
        var node = FindNodeViewModel(nodeId);
        if (node != null)
        {
            node.IsExpanded = false;
        }
    }

    /// <summary>
    /// Selects a node in the tree.
    /// </summary>
    /// <param name="nodeId">The ID of the node to select.</param>
    public void SelectNode(Guid nodeId)
    {
        var node = FindNodeViewModel(nodeId);
        if (node != null)
        {
            SelectedNode = node;
        }
    }

    private async Task CreateSceneAsync(string name)
    {
        try
        {
            var result = await _mediator.Send(new CreateSceneCommand(name));
            if (result.IsSuccess)
            {
                _sceneManager.SetCurrentScene(result.Value);
            }
        }
        catch (Exception ex)
        {
            // Handle error (could show a notification or log)
            System.Diagnostics.Debug.WriteLine($"Error creating scene: {ex.Message}");
        }
    }

    private async Task CreateLayerAsync(string name)
    {
        try
        {
            var result = await _mediator.Send(new CreateLayerCommand(name));
            if (result.IsSuccess)
            {
                // Layer will be added through scene modification event
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating layer: {ex.Message}");
        }
    }

    private async Task CreateGroupAsync(string name)
    {
        try
        {
            var layerId = SelectedLayer?.Layer.Id ?? CurrentScene?.GetDefaultLayer().Id;
            if (layerId.HasValue)
            {
                var result = _sceneManager.CreateGroup(name, layerId.Value);
                if (result.IsSuccess)
                {
                    // Group will be added through scene modification event
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating group: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task DeleteNodeAsync(Guid nodeId)
    {
        try
        {
            var result = _sceneManager.RemoveFromScene(nodeId);
            if (result.IsSuccess)
            {
                // Node will be removed through scene modification event
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting node: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task DeleteLayerAsync(Guid layerId)
    {
        try
        {
            var result = _sceneManager.RemoveLayer(layerId);
            if (result.IsSuccess)
            {
                // Layer will be removed through scene modification event
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting layer: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task MoveToLayerAsync((Guid nodeId, Guid layerId) args)
    {
        try
        {
            var result = _sceneManager.MoveToLayer(args.nodeId, args.layerId);
            if (result.IsSuccess)
            {
                // Node will be updated through scene modification event
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error moving node to layer: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private async Task RefreshAsync()
    {
        LoadCurrentScene();
        await Task.CompletedTask;
    }

    private void OnCurrentSceneChanged(object? sender, SceneChangedEventArgs e)
    {
        CurrentScene = e.NewScene;
        LoadCurrentScene();
    }

    private void OnSceneModified(object? sender, SceneModifiedEventArgs e)
    {
        // Refresh the tree when scene is modified
        LoadCurrentScene();
    }

    private void LoadCurrentScene()
    {
        RootNodes.Clear();
        Layers.Clear();

        if (CurrentScene == null)
            return;

        // Load layers
        foreach (var layer in CurrentScene.Layers)
        {
            Layers.Add(new LayerViewModel(layer));
        }

        // Load root nodes
        foreach (var node in CurrentScene.RootNodes)
        {
            RootNodes.Add(CreateNodeViewModel(node));
        }
    }

    private SceneNodeViewModel CreateNodeViewModel(SceneNode node)
    {
        return node switch
        {
            SceneObject obj => new SceneObjectViewModel(obj),
            SceneGroup group => new SceneGroupViewModel(group),
            _ => new SceneNodeViewModel(node)
        };
    }

    private SceneNodeViewModel? FindNodeViewModel(Guid nodeId)
    {
        return FindNodeViewModelRecursive(RootNodes, nodeId);
    }

    private SceneNodeViewModel? FindNodeViewModelRecursive(IEnumerable<SceneNodeViewModel> nodes, Guid nodeId)
    {
        foreach (var node in nodes)
        {
            if (node.Node.Id == nodeId)
                return node;

            var found = FindNodeViewModelRecursive(node.Children, nodeId);
            if (found != null)
                return found;
        }

        return null;
    }
}

/// <summary>
/// Base view model for scene nodes.
/// </summary>
public class SceneNodeViewModel : ViewModelBase
{
    private bool _isExpanded;
    private bool _isSelected;

    /// <summary>
    /// Initializes a new instance of the SceneNodeViewModel class.
    /// </summary>
    /// <param name="node">The scene node.</param>
    public SceneNodeViewModel(SceneNode node)
    {
        Node = node ?? throw new ArgumentNullException(nameof(node));
        Children = new ObservableCollection<SceneNodeViewModel>();

        // Load children
        foreach (var child in node.Children)
        {
            Children.Add(CreateChildViewModel(child));
        }
    }

    /// <summary>
    /// Gets the underlying scene node.
    /// </summary>
    public SceneNode Node { get; }

    /// <summary>
    /// Gets the node name.
    /// </summary>
    public string Name => Node.Name;

    /// <summary>
    /// Gets the node ID.
    /// </summary>
    public Guid Id => Node.Id;

    /// <summary>
    /// Gets or sets a value indicating whether the node is expanded in the tree.
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the node is selected in the tree.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    /// <summary>
    /// Gets a value indicating whether the node is visible.
    /// </summary>
    public bool IsVisible => Node.IsVisible;

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public ObservableCollection<SceneNodeViewModel> Children { get; }

    /// <summary>
    /// Gets a value indicating whether this node has children.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    private SceneNodeViewModel CreateChildViewModel(SceneNode child)
    {
        return child switch
        {
            SceneObject obj => new SceneObjectViewModel(obj),
            SceneGroup group => new SceneGroupViewModel(group),
            _ => new SceneNodeViewModel(child)
        };
    }
}

/// <summary>
/// View model for scene objects.
/// </summary>
public class SceneObjectViewModel : SceneNodeViewModel
{
    /// <summary>
    /// Initializes a new instance of the SceneObjectViewModel class.
    /// </summary>
    /// <param name="sceneObject">The scene object.</param>
    public SceneObjectViewModel(SceneObject sceneObject) : base(sceneObject)
    {
        SceneObject = sceneObject;
    }

    /// <summary>
    /// Gets the underlying scene object.
    /// </summary>
    public SceneObject SceneObject { get; }

    /// <summary>
    /// Gets the triangle count of the object.
    /// </summary>
    public int TriangleCount => SceneObject.Model.TriangleCount;

    /// <summary>
    /// Gets the material of the object.
    /// </summary>
    public SceneObjectMaterial Material => SceneObject.Material;
}

/// <summary>
/// View model for scene groups.
/// </summary>
public class SceneGroupViewModel : SceneNodeViewModel
{
    /// <summary>
    /// Initializes a new instance of the SceneGroupViewModel class.
    /// </summary>
    /// <param name="sceneGroup">The scene group.</param>
    public SceneGroupViewModel(SceneGroup sceneGroup) : base(sceneGroup)
    {
        SceneGroup = sceneGroup;
    }

    /// <summary>
    /// Gets the underlying scene group.
    /// </summary>
    public SceneGroup SceneGroup { get; }

    /// <summary>
    /// Gets the total triangle count of all objects in the group.
    /// </summary>
    public int TotalTriangleCount => GetTotalTriangleCount(SceneGroup);

    private int GetTotalTriangleCount(SceneGroup group)
    {
        int total = 0;
        foreach (var child in group.Children)
        {
            total += child switch
            {
                SceneObject obj => obj.Model.TriangleCount,
                SceneGroup childGroup => GetTotalTriangleCount(childGroup),
                _ => 0
            };
        }
        return total;
    }
}

/// <summary>
/// View model for layers.
/// </summary>
public class LayerViewModel : ViewModelBase
{
    /// <summary>
    /// Initializes a new instance of the LayerViewModel class.
    /// </summary>
    /// <param name="layer">The layer.</param>
    public LayerViewModel(Layer layer)
    {
        Layer = layer ?? throw new ArgumentNullException(nameof(layer));
    }

    /// <summary>
    /// Gets the underlying layer.
    /// </summary>
    public Layer Layer { get; }

    /// <summary>
    /// Gets the layer name.
    /// </summary>
    public string Name => Layer.Name;

    /// <summary>
    /// Gets the layer ID.
    /// </summary>
    public Guid Id => Layer.Id;

    /// <summary>
    /// Gets a value indicating whether the layer is visible.
    /// </summary>
    public bool IsVisible => Layer.IsVisible;

    /// <summary>
    /// Gets a value indicating whether the layer is locked.
    /// </summary>
    public bool IsLocked => Layer.IsLocked;

    /// <summary>
    /// Gets the layer color.
    /// </summary>
    public STLViewer.Math.Color Color => Layer.Color;

    /// <summary>
    /// Gets the layer opacity.
    /// </summary>
    public float Opacity => Layer.Opacity;

    /// <summary>
    /// Gets a value indicating whether this is the default layer.
    /// </summary>
    public bool IsDefault => Layer.IsDefault;
}
