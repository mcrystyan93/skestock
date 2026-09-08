using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Storage.Queries.GetFileDownload;

namespace skestock.Application.UnitTests.Features.Storage.Queries.GetFileDownload;

public class GetFileDownloadQueryValidatorTests
{
    private readonly GetFileDownloadQueryValidator _validator = new();

    [Test]
    public void Validate_WithEmptyId_Fails()
    {
        var result = _validator.Validate(new GetFileDownloadQuery { Id = Guid.Empty });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e =>
            e.PropertyName == nameof(GetFileDownloadQuery.Id)
            && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public void Validate_WithNonEmptyId_Passes()
    {
        var result = _validator.Validate(new GetFileDownloadQuery { Id = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }
}
