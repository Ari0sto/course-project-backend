using Microsoft.AspNetCore.Identity;

namespace TechStore.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
    }
}
