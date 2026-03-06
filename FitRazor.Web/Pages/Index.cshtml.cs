using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly FitRazorContext _context;
        public List<Trainer> Trainers { get; set; } = [];

        public IndexModel(FitRazorContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            Trainers = await _context.Trainers.ToListAsync();
        }
    }
}
