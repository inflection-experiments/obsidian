using MediatR;
using STLViewer.Domain.ValueObjects;

namespace STLViewer.Application.Queries;

/// <summary>
/// Query to get the list of recently opened files.
/// </summary>
public class GetRecentFilesQuery : IRequest<IReadOnlyList<Domain.ValueObjects.FileInfo>>
{
    /// <summary>
    /// Gets whether to refresh the files status before returning.
    /// </summary>
    public bool RefreshStatus { get; }

    public GetRecentFilesQuery(bool refreshStatus = false)
    {
        RefreshStatus = refreshStatus;
    }
}
