namespace skestock.Application.Features.EntityHistory.Queries.GetEntityHistory;

public enum EntityHistoryOperation
{
    Created,
    Updated,
    Deleted
}

public sealed record EntityHistoryVersionDto
{
    /// <summary>UTC instant this version became current.</summary>
    public required DateTimeOffset ValidFrom { get; init; }

    /// <summary>UTC instant this version was superseded; null for the version that is still current.</summary>
    public DateTimeOffset? ValidTo { get; init; }

    public required EntityHistoryOperation Operation { get; init; }

    /// <summary>Identity user who wrote this version; null means the system (jobs, seeding, anonymous sign-in).</summary>
    public Guid? ModifiedById { get; init; }

    /// <summary>The modifier's name as it was when this version was written, when a profile exists.</summary>
    public string? ModifiedByName { get; init; }

    public required IReadOnlyDictionary<string, object?> Values { get; init; }
}
