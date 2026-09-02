namespace skestock.Domain.Common;

/// <summary>
/// Marker interface for entities that support keyset pagination.
/// Implementing types must have Id and Created properties for cursor encoding.
/// </summary>
public interface IKeysetEntity
{
    Guid Id { get; }
    DateTimeOffset CreatedDate { get; }
}

