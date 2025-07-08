using MediatR;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to clear the recent files list.
/// </summary>
public class ClearRecentFilesCommand : IRequest
{
    /// <summary>
    /// Creates a new instance of the clear recent files command.
    /// </summary>
    public ClearRecentFilesCommand()
    {
    }
}
