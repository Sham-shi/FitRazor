using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities
{
    //[Authorize(Roles = "Trainer,Admin")]
    public class DetailsModel : PageModel
    {
        private readonly FitRazorContext _context;

        public DetailsModel(FitRazorContext context)
        {
            _context = context;
        }

        public dynamic? Entity { get; set; }
        public string EntityName { get; set; } = "";
        public int Id { get; set; }

        public async Task<IActionResult> OnGetAsync(string entityName, int id)
        {
            EntityName = entityName;
            Id = id;

            Entity = entityName switch
            {
                "Trainers" => await _context.Trainers.FindAsync(id),
                "Clients" => await _context.Clients.FindAsync(id),
                "Services" => await _context.Services.FindAsync(id),
                "Bookings" => await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.TrainerService)
                    .FirstOrDefaultAsync(b => b.BookingId == id),
                "TrainerServices" => await _context.TrainerServices
                    .Include(ts => ts.Trainer)
                    .Include(ts => ts.Service)
                    .FirstOrDefaultAsync(ts => ts.TrainerServiceId == id),
                _ => null
            };

            if (Entity == null) return NotFound();

            return Page();
        }
    }
}