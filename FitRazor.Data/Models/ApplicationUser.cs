using Microsoft.AspNetCore.Identity;

namespace FitRazor.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
