using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Dashboard
{
    [Authorize(Roles = "Client")]
    public class ClientModel : PageModel
    {
        private readonly FitRazorContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ClientModel> _logger;

        public ClientModel(
            FitRazorContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ClientModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public Client? ClientProfile { get; set; }
        public ApplicationUser? UserProfile { get; set; }
        public List<Booking> Bookings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login");

            // Получаем профиль пользователя
            UserProfile = await _userManager.FindByIdAsync(userId);
            if (UserProfile == null || UserProfile.ClientId == null)
            {
                _logger.LogWarning("Клиент не найден для пользователя {UserId}", userId);
                return RedirectToPage("/Index");
            }

            // Получаем профиль клиента с бронированиями
            ClientProfile = await _context.Clients
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.TrainerService)
                        .ThenInclude(ts => ts!.Trainer)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.TrainerService)
                        .ThenInclude(ts => ts!.Service)
                .FirstOrDefaultAsync(c => c.ClientId == UserProfile.ClientId);

            if (ClientProfile == null)
            {
                _logger.LogWarning("Профиль клиента не найден {ClientId}", UserProfile.ClientId);
                return RedirectToPage("/Index");
            }

            // Сортируем бронирования по дате (сначала будущие)
            Bookings = ClientProfile.Bookings
                .OrderByDescending(b => b.BookingDateTime)
                .ToList();

            _logger.LogInformation("Дашборд клиента {ClientId} загружен", ClientProfile.ClientId);
            return Page();
        }
    }
}