using skestock.Application.Common.Interfaces;

namespace Worker.Services;

/// <summary>
/// Mutable, scoped <see cref="IUser"/> holder for the Worker. A background worker has no
/// HTTP context, so the acting user is supplied per message: the queue processor sets
/// <see cref="Id"/> from the message envelope (originally captured at enqueue time) inside
/// the per-message DI scope, before dispatching commands through Mediator.
/// </summary>
public class AmbientUser : IUser
{
    public Guid? Id { get; set; }
    public List<string>? Roles { get; set; }
}
