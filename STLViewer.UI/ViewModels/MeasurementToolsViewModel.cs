using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using MediatR;
using STLViewer.Application.Commands;
using STLViewer.Application.Queries;
using STLViewer.Domain.ValueObjects;
using STLViewer.Domain.Enums;
using STLViewer.Math;
using System.Linq;
using ReactiveUnit = System.Reactive.Unit;

namespace STLViewer.UI.ViewModels;

/// <summary>
/// View model for measurement tools functionality.
/// </summary>
public class MeasurementToolsViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private ObservableCollection<MeasurementSession> _measurementSessions = new();
    private MeasurementSession? _currentSession;
    private MeasurementType _selectedMeasurementType = MeasurementType.Distance;
    private string _selectedUnit = "mm";
    private Vector3 _point1 = Vector3.Zero;
    private Vector3 _point2 = Vector3.Zero;
    private Vector3 _point3 = Vector3.Zero;
    private string _currentMeasurementResult = string.Empty;
    private bool _isAnalyzing = false;

    public MeasurementToolsViewModel(IMediator mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        InitializeCommands();
        _ = LoadMeasurementSessionsAsync();
    }

    /// <summary>
    /// Gets the collection of measurement sessions.
    /// </summary>
    public ObservableCollection<MeasurementSession> MeasurementSessions
    {
        get => _measurementSessions;
        private set => this.RaiseAndSetIfChanged(ref _measurementSessions, value);
    }

    /// <summary>
    /// Gets or sets the current measurement session.
    /// </summary>
    public MeasurementSession? CurrentSession
    {
        get => _currentSession;
        set => this.RaiseAndSetIfChanged(ref _currentSession, value);
    }

    /// <summary>
    /// Gets or sets the selected measurement type.
    /// </summary>
    public MeasurementType SelectedMeasurementType
    {
        get => _selectedMeasurementType;
        set => this.RaiseAndSetIfChanged(ref _selectedMeasurementType, value);
    }

    /// <summary>
    /// Gets or sets the selected unit of measurement.
    /// </summary>
    public string SelectedUnit
    {
        get => _selectedUnit;
        set => this.RaiseAndSetIfChanged(ref _selectedUnit, value);
    }

    /// <summary>
    /// Gets or sets the first measurement point.
    /// </summary>
    public Vector3 Point1
    {
        get => _point1;
        set => this.RaiseAndSetIfChanged(ref _point1, value);
    }

    /// <summary>
    /// Gets or sets the second measurement point.
    /// </summary>
    public Vector3 Point2
    {
        get => _point2;
        set => this.RaiseAndSetIfChanged(ref _point2, value);
    }

    /// <summary>
    /// Gets or sets the third measurement point (for angle measurements).
    /// </summary>
    public Vector3 Point3
    {
        get => _point3;
        set => this.RaiseAndSetIfChanged(ref _point3, value);
    }

    /// <summary>
    /// Gets or sets the current measurement result display.
    /// </summary>
    public string CurrentMeasurementResult
    {
        get => _currentMeasurementResult;
        set => this.RaiseAndSetIfChanged(ref _currentMeasurementResult, value);
    }

    /// <summary>
    /// Gets or sets whether an analysis is currently running.
    /// </summary>
    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        set => this.RaiseAndSetIfChanged(ref _isAnalyzing, value);
    }

    /// <summary>
    /// Gets the available measurement types.
    /// </summary>
    public MeasurementType[] MeasurementTypes { get; } = new[]
    {
        MeasurementType.Distance,
        MeasurementType.Angle,
        MeasurementType.Volume,
        MeasurementType.SurfaceArea,
        MeasurementType.BoundingBox,
        MeasurementType.Centroid
    };

    /// <summary>
    /// Gets the available units of measurement.
    /// </summary>
    public string[] Units { get; } = new[]
    {
        "mm", "cm", "m", "inches", "feet", "units"
    };

    /// <summary>
    /// Gets whether the third point is required for the current measurement type.
    /// </summary>
    public bool RequiresThirdPoint => SelectedMeasurementType == MeasurementType.Angle;

    /// <summary>
    /// Gets whether points are required for the current measurement type.
    /// </summary>
    public bool RequiresPoints => SelectedMeasurementType == MeasurementType.Distance ||
                                  SelectedMeasurementType == MeasurementType.Angle;

    /// <summary>
    /// Command to measure distance between two points.
    /// </summary>
    public ReactiveCommand<ReactiveUnit, ReactiveUnit> MeasureDistanceCommand { get; private set; } = null!;

    /// <summary>
    /// Command to measure angle between three points.
    /// </summary>
    public ReactiveCommand<ReactiveUnit, ReactiveUnit> MeasureAngleCommand { get; private set; } = null!;

    /// <summary>
    /// Command to analyze the current model.
    /// </summary>
    public ReactiveCommand<Guid, ReactiveUnit> AnalyzeModelCommand { get; private set; } = null!;

    /// <summary>
    /// Command to create a new measurement session.
    /// </summary>
    public ReactiveCommand<ReactiveUnit, ReactiveUnit> CreateNewSessionCommand { get; private set; } = null!;

    /// <summary>
    /// Command to delete a measurement session.
    /// </summary>
    public ReactiveCommand<MeasurementSession, ReactiveUnit> DeleteSessionCommand { get; private set; } = null!;

    /// <summary>
    /// Command to refresh the measurement sessions list.
    /// </summary>
    public ReactiveCommand<ReactiveUnit, ReactiveUnit> RefreshSessionsCommand { get; private set; } = null!;

    /// <summary>
    /// Command to clear the current measurement result.
    /// </summary>
    public ReactiveCommand<ReactiveUnit, ReactiveUnit> ClearResultCommand { get; private set; } = null!;

    private void InitializeCommands()
    {
        MeasureDistanceCommand = ReactiveCommand.CreateFromTask(MeasureDistanceAsync);
        MeasureAngleCommand = ReactiveCommand.CreateFromTask(MeasureAngleAsync);
        AnalyzeModelCommand = ReactiveCommand.CreateFromTask<Guid>(AnalyzeModelAsync);
        CreateNewSessionCommand = ReactiveCommand.CreateFromTask(CreateNewSessionAsync);
        DeleteSessionCommand = ReactiveCommand.CreateFromTask<MeasurementSession>(DeleteSessionAsync);
        RefreshSessionsCommand = ReactiveCommand.CreateFromTask(LoadMeasurementSessionsAsync);
        ClearResultCommand = ReactiveCommand.Create(() => { ClearResult(); return ReactiveUnit.Default; });
    }

    private async Task MeasureDistanceAsync()
    {
        try
        {
            var command = new MeasureDistanceCommand(Point1, Point2, SelectedUnit);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                var measurement = result.Value;
                CurrentMeasurementResult = $"Distance: {measurement.FormattedValue}\n{measurement.Description}";

                // Add to current session if one exists
                if (CurrentSession != null)
                {
                    var updatedSession = CurrentSession.AddMeasurement(measurement);
                    UpdateSession(updatedSession);
                }
            }
            else
            {
                CurrentMeasurementResult = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            CurrentMeasurementResult = $"Error measuring distance: {ex.Message}";
        }
    }

    private async Task MeasureAngleAsync()
    {
        try
        {
            var command = new MeasureAngleCommand(Point2, Point1, Point3); // Point2 is the vertex
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                var measurement = result.Value;
                CurrentMeasurementResult = $"Angle: {measurement.FormattedValue}\n{measurement.Description}";

                // Add to current session if one exists
                if (CurrentSession != null)
                {
                    var updatedSession = CurrentSession.AddMeasurement(measurement);
                    UpdateSession(updatedSession);
                }
            }
            else
            {
                CurrentMeasurementResult = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            CurrentMeasurementResult = $"Error measuring angle: {ex.Message}";
        }
    }

    private async Task AnalyzeModelAsync(Guid modelId)
    {
        try
        {
            IsAnalyzing = true;
            CurrentMeasurementResult = "Analyzing model...";

            var command = new AnalyzeModelCommand(modelId, SelectedUnit, true);
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                var session = result.Value;
                CurrentSession = session;

                // Add to sessions collection if not already there
                if (!MeasurementSessions.Any(s => s.Id == session.Id))
                {
                    MeasurementSessions.Add(session);
                }

                // Display analysis summary
                var summary = $"Model Analysis Complete:\n\n{session.Summary}\n\nDetailed Results:\n";
                foreach (var measurement in session.Measurements)
                {
                    summary += $"• {measurement.Description}: {measurement.FormattedValue}\n";
                }

                CurrentMeasurementResult = summary;
            }
            else
            {
                CurrentMeasurementResult = $"Error: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            CurrentMeasurementResult = $"Error analyzing model: {ex.Message}";
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    private Task CreateNewSessionAsync()
    {
        try
        {
            var newSession = MeasurementSession.CreateDefault();
            CurrentSession = newSession;

            if (!MeasurementSessions.Any(s => s.Id == newSession.Id))
            {
                MeasurementSessions.Add(newSession);
            }

            CurrentMeasurementResult = $"Created new measurement session: {newSession.Name}";
        }
        catch (Exception ex)
        {
            CurrentMeasurementResult = $"Error creating new session: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private Task DeleteSessionAsync(MeasurementSession session)
    {
        try
        {
            if (session != null)
            {
                MeasurementSessions.Remove(session);

                if (CurrentSession?.Id == session.Id)
                {
                    CurrentSession = null;
                }

                CurrentMeasurementResult = $"Deleted measurement session: {session.Name}";
            }
        }
        catch (Exception ex)
        {
            CurrentMeasurementResult = $"Error deleting session: {ex.Message}";
        }

        return Task.CompletedTask;
    }

    private async Task LoadMeasurementSessionsAsync()
    {
        try
        {
            var query = new GetMeasurementSessionsQuery();
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                MeasurementSessions.Clear();
                foreach (var session in result.Value)
                {
                    MeasurementSessions.Add(session);
                }
            }
        }
        catch (Exception ex)
        {
            CurrentMeasurementResult = $"Error loading sessions: {ex.Message}";
        }
    }

    private void ClearResult()
    {
        CurrentMeasurementResult = string.Empty;
    }

    /// <summary>
    /// Helper method to update a session in both the current session and the collection.
    /// </summary>
    /// <param name="updatedSession">The updated session instance.</param>
    private void UpdateSession(MeasurementSession updatedSession)
    {
        if (CurrentSession?.Id == updatedSession.Id)
        {
            CurrentSession = updatedSession;
        }

        // Update in the collection
        var existingIndex = -1;
        for (int i = 0; i < MeasurementSessions.Count; i++)
        {
            if (MeasurementSessions[i].Id == updatedSession.Id)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            MeasurementSessions[existingIndex] = updatedSession;
        }
        else
        {
            MeasurementSessions.Add(updatedSession);
        }
    }
}
