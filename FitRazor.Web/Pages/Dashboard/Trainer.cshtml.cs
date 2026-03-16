using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Dashboard
{
    [Authorize(Roles = "Trainer")]
    public class TrainerModel : PageModel
    {
        private readonly FitRazorContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TrainerModel> _logger;

        public TrainerModel(
            FitRazorContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<TrainerModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public Trainer? TrainerProfile { get; set; }
        public ApplicationUser? UserProfile { get; set; }
        public List<Booking> Bookings { get; set; } = new();
        public List<TrainerService> Services { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            // Получаем профиль пользователя
            UserProfile = await _userManager.FindByIdAsync(userId);
            if (UserProfile == null || UserProfile.TrainerId == null)
            {
                _logger.LogWarning("Тренер не найден для пользователя {UserId}", userId);
                return RedirectToPage("/Index");
            }

            // Получаем профиль тренера
            TrainerProfile = await _context.Trainers
                .Include(t => t.TrainerServices)
                    .ThenInclude(ts => ts.Service)
                .FirstOrDefaultAsync(t => t.TrainerId == UserProfile.TrainerId);

            if (TrainerProfile == null)
            {
                _logger.LogWarning("Профиль тренера не найден {TrainerId}", UserProfile.TrainerId);
                return RedirectToPage("/Index");
            }

            // Получаем услуги тренера
            Services = TrainerProfile.TrainerServices.ToList();

            // Получаем бронирования через услуги тренера
            var trainerServiceIds = Services.Select(ts => ts.TrainerServiceId).ToList();
            Bookings = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.TrainerService)
                    .ThenInclude(ts => ts!.Service)
                .Where(b => trainerServiceIds.Contains(b.TrainerServiceId))
                .OrderByDescending(b => b.BookingDateTime)
                .ToListAsync();

            _logger.LogInformation("Дашборд тренера {TrainerId} загружен", TrainerProfile.TrainerId);
            return Page();
        }
    }
}