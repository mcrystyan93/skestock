using skestock.Application.Common.Exceptions;
using skestock.Application.Queues;
using NUnit.Framework;
using Shouldly;
using ValidationException = skestock.Application.Common.Exceptions.ValidationException;

namespace skestock.Application.UnitTests.Queues;

public class MessageFailureClassifierTests
{
    [TestCase(typeof(ValidationException))]
    [TestCase(typeof(UnauthorizedAccessException))]
    [TestCase(typeof(ForbiddenAccessException))]
    [TestCase(typeof(UnprocessableDocumentException))]
    [TestCase(typeof(ArgumentException))]
    public void IsPermanent_ForKnownPermanentExceptionTypes_ReturnsTrue(Type exceptionType)
    {
        var exception = CreateException(exceptionType);

        MessageFailureClassifier.IsPermanent(exception).ShouldBeTrue();
    }

    [Test]
    public void IsPermanent_ForTransientExtractionException_ReturnsFalse()
    {
        var exception = new TransientExtractionException("network blip");

        MessageFailureClassifier.IsPermanent(exception).ShouldBeFalse();
    }

    [TestCase(typeof(InvalidOperationException))]
    [TestCase(typeof(TimeoutException))]
    [TestCase(typeof(Exception))]
    public void IsPermanent_ForUnclassifiedExceptionTypes_ReturnsFalse(Type exceptionType)
    {
        var exception = CreateException(exceptionType);

        MessageFailureClassifier.IsPermanent(exception).ShouldBeFalse();
    }

    private static Exception CreateException(Type exceptionType)
    {
        if (exceptionType == typeof(ValidationException)) return new ValidationException();
        if (exceptionType == typeof(UnprocessableDocumentException))
            return new UnprocessableDocumentException("unreadable file");

        return (Exception)Activator.CreateInstance(exceptionType)!;
    }
}
