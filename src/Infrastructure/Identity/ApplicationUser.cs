using Microsoft.AspNetCore.Identity;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        // Time-ordered GUID v7 so Identity user ids stay monotonic like the domain keys.
        Id = Guid.CreateVersion7();
    }

    public UserProfile? UserProfile { get; set; }
}
