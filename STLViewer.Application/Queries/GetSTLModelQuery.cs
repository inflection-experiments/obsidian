using MediatR;
using STLViewer.Application.Commands;
using STLViewer.Application.DTOs;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Queries;

public class GetSTLModelQuery : IRequest<Result<STLModelDto>>
{
    public string FilePath { get; set; } = string.Empty;
    public bool LoadTriangles { get; set; } = true;
    public bool ValidateModel { get; set; } = false;
    public bool LoadMetadata { get; set; } = true;
}

public class GetSTLModelQueryHandler : IRequestHandler<GetSTLModelQuery, Result<STLModelDto>>
{
    private readonly IMediator _mediator;

    public GetSTLModelQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<Result<STLModelDto>> Handle(GetSTLModelQuery request, CancellationToken cancellationToken)
    {
        // Reuse the LoadSTLCommand for actual loading
        var loadCommand = new LoadSTLCommand
        {
            FilePath = request.FilePath,
            LoadTriangles = request.LoadTriangles,
            ValidateModel = request.ValidateModel
        };

        var result = await _mediator.Send(loadCommand, cancellationToken);

        if (result.IsFailure)
            return Result<STLModelDto>.Fail(result.Error);

        var modelDto = result.Value;

        // If not loading triangles, clear them to save memory
        if (!request.LoadTriangles)
        {
            modelDto.Triangles.Clear();
        }

        // If not loading metadata, clear it
        if (!request.LoadMetadata)
        {
            modelDto.Metadata = null;
        }

        return Result.Ok(modelDto);
    }
}
