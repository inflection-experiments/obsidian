using MediatR;
using STLViewer.Domain.Common;
using STLViewer.Domain.Entities;

namespace STLViewer.Application.Commands;

/// <summary>
/// Command to load multiple STL files.
/// </summary>
public class LoadFilesCommand : IRequest<Result<IReadOnlyList<STLModel>>>
{
    /// <summary>
    /// Gets the file paths to load.
    /// </summary>
    public IReadOnlyList<string> FilePaths { get; }

    /// <summary>
    /// Gets whether to validate files before loading.
    /// </summary>
    public bool ValidateFirst { get; }

    /// <summary>
    /// Gets whether to continue loading if some files fail.
    /// </summary>
    public bool ContinueOnError { get; }

    public LoadFilesCommand(IEnumerable<string> filePaths, bool validateFirst = true, bool continueOnError = true)
    {
        FilePaths = filePaths.ToList();
        ValidateFirst = validateFirst;
        ContinueOnError = continueOnError;
    }
}
