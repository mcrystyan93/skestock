using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.GetTopDonations;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetTopDonations;

[TestFixture]
public sealed class GetTopDonationsQueryValidatorTests
{
    private readonly GetTopDonationsQueryValidator _validator = new();

    [Test]
    public void Defaults_are_valid() => _validator.Validate(new GetTopDonationsQuery()).IsValid.ShouldBeTrue();

    [Test]
    public void Class_scope_requires_class_id() =>
        _validator.Validate(new GetTopDonationsQuery { Scope = PurchaseStatisticsScope.Class })
            .Errors.ShouldContain(error => error.PropertyName == nameof(GetTopDonationsQuery.ClassId));

    [TestCase(0)]
    [TestCase(GetTopDonationsQuery.MaxTop + 1)]
    public void Top_out_of_range_fails(int top) =>
        _validator.Validate(new GetTopDonationsQuery { Top = top }).IsValid.ShouldBeFalse();
}
