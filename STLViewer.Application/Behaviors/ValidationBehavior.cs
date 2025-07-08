using FluentValidation;
using MediatR;
using STLViewer.Domain.Common;

namespace STLViewer.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

                if (failures.Any())
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));

            // Create a failed result of the expected type
            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = resultType.GetGenericArguments()[0];
                var failMethod = typeof(Result<>).MakeGenericType(valueType).GetMethod("Fail", new[] { typeof(string) });
                return (TResponse)failMethod?.Invoke(null, new object[] { errorMessage })!;
            }
            else if (resultType == typeof(Result))
            {
                return (TResponse)(object)Result.Fail(errorMessage);
            }
            else
            {
                // Not a Result type, proceed normally
                return await next();
            }
        }

        return await next();
    }
}
