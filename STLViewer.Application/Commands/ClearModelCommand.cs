using MediatR;
using Microsoft.Extensions.Logging;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Commands;

public class ClearModelCommand : IRequest<Result>
{
    public bool ConfirmClear { get; set; } = false;
}

public class ClearModelCommandHandler : IRequestHandler<ClearModelCommand, Result>
{
    private readonly ILogger<ClearModelCommandHandler> _logger;

    public ClearModelCommandHandler(ILogger<ClearModelCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> Handle(ClearModelCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Clearing currently loaded STL model");

            // This would typically clear the model from some state management service
            // For now, this is just a placeholder that shows the pattern

            await Task.CompletedTask; // Placeholder for async operations

            _logger.LogInformation("Successfully cleared STL model");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing STL model");
            return Result.Fail($"Error clearing model: {ex.Message}");
        }
    }
}
