using FluentResults;
using Moq;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

namespace skestock.Application.UnitTests.Features.EntityHistory.Queries.GetEntityHistory;

public class GetEntityHistoryTests
{
    private static EntityHistoryVersionDto Version() => new()
    {
        ValidFrom = DateTimeOffset.UtcNow,
        Operation = EntityHistoryOperation.Updated,
        Values = new Dictionary<string, object?>()
    };

    [TestCase("Items", "abc", null, 10, true)]
    [TestCase("", "abc", null, 10, false)]
    [TestCase("Items", "", null, 10, false)]
    [TestCase("Items", "abc", null, 0, false)]
    [TestCase("Items", "abc", null, 51, false)]
    [TestCase("Items", "abc", "-1", 10, false)]
    [TestCase("Items", "abc", "x", 10, false)]
    [TestCase("Items", "abc", "20", 10, true)]
    [TestCase("Items", "abc", "1000000", 10, true)]
    [TestCase("Items", "abc", "1000001", 10, false)]
    [TestCase("Items", "abc", "2147483647", 10, false)]
    public void Validator(string entity, string id, string? cursor, int pageSize, bool valid)
    {
        var result = new GetEntityHistoryQueryValidator().Validate(new GetEntityHistoryQuery
        {
            Entity = entity, Id = id, Cursor = cursor, PageSize = pageSize
        });

        result.IsValid.ShouldBe(valid);
    }

    [Test]
    public async Task ShouldReturnNextCursorAsTheNextOffsetWhenMoreVersionsExist()
    {
        var reader = new Mock<IEntityHistoryReader>();
        reader.Setup(r => r.ReadAsync("Items", "1", 20, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new EntityHistoryPage([Version(), Version()], true)));

        var result = await new GetEntityHistoryHandler(reader.Object).Handle(
            new GetEntityHistoryQuery { Entity = "Items", Id = "1", Cursor = "20", PageSize = 2 }, CancellationToken.None);

        result.Value.NextCursor.ShouldBe("22");
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(2);
    }

    [Test]
    public async Task ShouldOmitNextCursorOnTheLastPage()
    {
        var reader = new Mock<IEntityHistoryReader>();
        reader.Setup(r => r.ReadAsync("Items", "1", 0, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new EntityHistoryPage([Version()], false)));

        var result = await new GetEntityHistoryHandler(reader.Object).Handle(
            new GetEntityHistoryQuery { Entity = "Items", Id = "1", PageSize = 5 }, CancellationToken.None);

        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task ShouldPassReaderFailuresThrough()
    {
        var reader = new Mock<IEntityHistoryReader>();
        reader.Setup(r => r.ReadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Fail<EntityHistoryPage>(new EntityHistoryErrors.UnknownEntity("x")));

        var result = await new GetEntityHistoryHandler(reader.Object).Handle(
            new GetEntityHistoryQuery { Entity = "x", Id = "1" }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors[0].ShouldBeOfType<EntityHistoryErrors.UnknownEntity>();
    }
}
