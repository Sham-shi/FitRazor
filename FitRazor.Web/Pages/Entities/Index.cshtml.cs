using FitRazor.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities
{
    public class IndexModel : PageModel
    {
        private readonly FitRazorContext _context;

        public IndexModel(FitRazorContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string EntityName { get; set; } = "Trainers";

        public void OnGet(string entityName)
        {
            EntityName = entityName ?? "Trainers";

            // Валидация имени сущности
            var validEntities = new[] { "Trainers", "Clients", "Services", "Bookings", "TrainerServices" };
            if (!validEntities.Contains(EntityName))
            {
                EntityName = "Trainers";
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string entityName, int id)
        {
            try
            {
                var deleted = entityName switch
                {
                    "Trainers" => await DeleteTrainerAsync(id),
                    "Clients" => await DeleteClientAsync(id),
                    "Services" => await DeleteServiceAsync(id),
                    "Bookings" => await DeleteBookingAsync(id),
                    "TrainerServices" => await DeleteTrainerServiceAsync(id),
                    _ => false
                };

                if (deleted)
                {
                    TempData["SuccessMessage"] = "Запись успешно удалена!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Запись не найдена или уже удалена";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при удалении: {ex.Message}";
            }

            return RedirectToPage("Index", new { entityName = entityName });
        }

        private async Task<bool> DeleteTrainerAsync(int id)
        {
            var trainer = await _context.Trainers.FindAsync(id);
            if (trainer == null) return false;

            // Проверка на связанные записи
            var hasBookings = await _context.TrainerServices
                .AnyAsync(ts => ts.TrainerId == id);

            if (hasBookings)
            {
                throw new Exception("Нельзя удалить тренера: есть связанные услуги");
            }

            _context.Trainers.Remove(trainer);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> DeleteClientAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return false;

            var hasBookings = await _context.Bookings.AnyAsync(b => b.ClientId == id);
            if (hasBookings)
            {
                throw new Exception("Нельзя удалить клиента: есть бронирования");
            }

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> DeleteServiceAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return false;

            var hasTrainerServices = await _context.TrainerServices.AnyAsync(ts => ts.ServiceId == id);
            if (hasTrainerServices)
            {
                throw new Exception("Нельзя удалить услугу: назначена тренерам");
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> DeleteBookingAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return false;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> DeleteTrainerServiceAsync(int id)
        {
            var trainerService = await _context.TrainerServices.FindAsync(id);
            if (trainerService == null) return false;

            var hasBookings = await _context.Bookings.AnyAsync(b => b.TrainerServiceId == id);
            if (hasBookings)
            {
                throw new Exception("Нельзя удалить связь: есть бронирования на эту услугу");
            }

            _context.TrainerServices.Remove(trainerService);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
