using ValidationException = skestock.Application.Common.Exceptions.ValidationException;

namespace skestock.Application.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var validationResults = await Task.WhenAll(
                validators.Select(v =>
                    v.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next(request, cancellationToken);
    }
}
