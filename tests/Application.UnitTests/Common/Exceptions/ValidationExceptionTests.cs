using skestock.Application.Common.Errors;
using skestock.Application.Common.Exceptions;
using FluentValidation.Results;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Common.Exceptions;

public class ValidationExceptionTests
{
    [Test]
    public void DefaultConstructorCreatesAnEmptyErrorDictionary()
    {
        var actual = new ValidationException().Errors;

        actual.Keys.ShouldBeEmpty();
    }

    [Test]
    public void SingleValidationFailureCreatesASingleElementErrorDictionary()
    {
        var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Age", "must be over 18") { ErrorCode = "GreaterThanOrEqualValidator" },
            };

        var actual = new ValidationException(failures).Errors;

        actual.Keys.ShouldBe(new string[] { "Age" });
        actual["Age"].Select(e => e.Code).ShouldBe(new[] { ValidationErrorCodes.GreaterThanOrEqualTo });
    }

    [Test]
    public void MulitpleValidationFailureForMultiplePropertiesCreatesAMultipleElementErrorDictionaryEachWithMultipleValues()
    {
        var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Age", "must be 18 or older") { ErrorCode = "GreaterThanOrEqualValidator" },
                new ValidationFailure("Age", "must be 25 or younger") { ErrorCode = "LessThanOrEqualValidator" },
                new ValidationFailure("Password", "must contain at least 8 characters") { ErrorCode = "MinimumLengthValidator" },
                new ValidationFailure("Password", "must contain a digit") { ErrorCode = "validation.password_digit" },
                new ValidationFailure("Password", "must contain upper case letter") { ErrorCode = "validation.password_uppercase" },
                new ValidationFailure("Password", "must contain lower case letter") { ErrorCode = "validation.password_lowercase" },
            };

        var actual = new ValidationException(failures).Errors;

        actual.Keys.ShouldBe(new string[] { "Password", "Age" }, ignoreOrder: true);

        actual["Age"].Select(e => e.Code).ShouldBe(new[]
        {
                ValidationErrorCodes.LessThanOrEqualTo,
                ValidationErrorCodes.GreaterThanOrEqualTo,
        }, ignoreOrder: true);

        actual["Password"].Select(e => e.Code).ShouldBe(new[]
        {
                "validation.password_lowercase",
                "validation.password_uppercase",
                ValidationErrorCodes.MinLength,
                "validation.password_digit",
        }, ignoreOrder: true);
    }

    [Test]
    public void FailureWithNoErrorCodeMapsToUnknownCode()
    {
        var failures = new List<ValidationFailure>
            {
                new ValidationFailure("Age", "must be over 18"),
            };

        var actual = new ValidationException(failures).Errors;

        actual["Age"].Single().Code.ShouldBe(ValidationErrorCodes.Unknown);
    }

    [Test]
    public void FailureWithMaxLengthPlaceholderExtractsMaxLengthParam()
    {
        var failure = new ValidationFailure("Name", "too long")
        {
            ErrorCode = "MaximumLengthValidator",
            FormattedMessagePlaceholderValues = new Dictionary<string, object>
            {
                ["MaxLength"] = 50,
            },
        };

        var actual = new ValidationException(new[] { failure }).Errors;

        var error = actual["Name"].Single();
        error.Code.ShouldBe(ValidationErrorCodes.MaxLength);
        error.Params["maxLength"].ShouldBe(50);
    }
}
