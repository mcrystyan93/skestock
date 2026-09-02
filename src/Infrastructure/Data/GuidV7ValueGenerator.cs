using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace skestock.Infrastructure.Data;

/// <summary>
/// Generates a time-ordered GUID v7 (<see cref="Guid.CreateVersion7()"/>) for a key the moment an
/// entity enters the <c>Added</c> state, not at <c>SaveChanges</c> time. Assigning the id at track
/// time is essential: with a late (SaveChanges) assignment the key stays <see cref="Guid.Empty"/>
/// in the change tracker, so adding two entities of the same type before saving collides on the
/// empty key ("another instance with the same key value is already being tracked"). The generated
/// value is permanent (<see cref="GeneratesTemporaryValues"/> == <c>false</c>) and monotonic, which
/// also keeps keyset-pagination ordering on Id stable.
/// </summary>
public sealed class GuidV7ValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
}
