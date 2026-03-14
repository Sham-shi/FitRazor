using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitRazor.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime? LastLoginDate { get; set; }

        [ForeignKey("Client")]
        public int? ClientId { get; set; }
        public virtual Client? ClientProfile { get; set; }

        [ForeignKey("Trainer")]
        public int? TrainerId { get; set; }
        public virtual Trainer? TrainerProfile { get; set; }
    }
}
