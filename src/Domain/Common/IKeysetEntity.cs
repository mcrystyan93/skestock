namespace skestock.Domain.Common;

/// <summary>
/// Marker interface for entities that support keyset pagination.
/// Implementing types must have Id and Created properties for cursor encoding.
/// </summary>
public interface IKeysetEntity
{
    int Id { get; }
    DateTimeOffset CreatedDate { get; }
}

