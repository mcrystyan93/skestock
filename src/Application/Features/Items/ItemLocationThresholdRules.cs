using System.Linq.Expressions;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items;

internal static class ItemLocationThresholdRules
{
    public static void Add<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, List<ItemLocationThresholdDto>>> thresholds,
        IApplicationDbContext dbContext)
    {
        validator.RuleFor(thresholds)
            .NotNull()
            .WithErrorCode(ValidationErrorCodes.Required)
            .DependentRules(() =>
            {
                validator.RuleFor(thresholds)
                    .Must(list => list.All(t => t.MinThreshold >= 0))
                    .WithMessage("Thresholds must be zero or greater")
                    .WithErrorCode(ValidationErrorCodes.GreaterThanOrEqualTo);

                validator.RuleFor(thresholds)
                    .Must(list => list.Select(t => t.LocationId).Distinct().Count() == list.Count)
                    .WithMessage("Each location can have only one threshold")
                    .WithErrorCode(ValidationErrorCodes.DuplicateLocation);

                validator.RuleFor(thresholds)
                    .MustAsync(async (list, cancellationToken) =>
                    {
                        var ids = list.Select(t => t.LocationId).Distinct().ToList();
                        return await dbContext.Locations.AsNoTracking()
                            .CountAsync(l => ids.Contains(l.Id), cancellationToken) == ids.Count;
                    })
                    .WithMessage("Location does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }
}
