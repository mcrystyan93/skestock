using FluentValidation.Results;
using skestock.Application.Common.Errors;

namespace skestock.Application.Common.Exceptions;
public record ValidationFieldError(string Code, IReadOnlyDictionary<string, object> Params);
public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, ValidationFieldError[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ValidationCodeMapper.Map).ToArray());
    }

    public IDictionary<string, ValidationFieldError[]> Errors { get; }
}
