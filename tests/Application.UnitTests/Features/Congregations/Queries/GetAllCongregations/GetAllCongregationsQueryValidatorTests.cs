using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Models;
using skestock.Application.Features.Congregations.Queries.GetAllCongregations;

namespace skestock.Application.UnitTests.Features.Congregations.Queries.GetAllCongregations;

public class GetAllCongregationsQueryValidatorTests
{
    private readonly GetAllCongregationsQueryValidator _validator = new();

    [Test]
    public void DefaultQuery_IsValid() =>
        _validator.Validate(new GetAllCongregationsQuery()).IsValid.ShouldBeTrue();

    [TestCase(0)]
    [TestCase(PaginationConstants.DEFAULT_PAGE_SIZE + 1)]
    public void PageSizeOutOfRange_HasBetweenError(int pageSize)
    {
        var result = _validator.Validate(new GetAllCongregationsQuery { PageSize = pageSize });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Between);
    }

    [Test]
    public void UnknownSortKey_HasInvalidSortKeyError()
    {
        var result = _validator.Validate(new GetAllCongregationsQuery
        {
            Sort = [new PaginationSort { Key = "email", Value = "asc" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidSortKey);
    }

    [Test]
    public void UnknownSortDirection_HasInvalidSortDirectionError()
    {
        var result = _validator.Validate(new GetAllCongregationsQuery
        {
            Sort = [new PaginationSort { Key = "name", Value = "sideways" }]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidSortDirection);
    }

    [Test]
    public void MalformedCursor_HasInvalidCursorError()
    {
        var result = _validator.Validate(new GetAllCongregationsQuery { Cursor = "not-a-cursor" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidCursor);
    }
}
