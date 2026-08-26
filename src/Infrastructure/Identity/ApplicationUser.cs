using Microsoft.AspNetCore.Identity;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public UserProfile? UserProfile { get; set; }
}
