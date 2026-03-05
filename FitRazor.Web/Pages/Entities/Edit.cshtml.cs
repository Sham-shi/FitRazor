using FitRazor.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities
{
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly FitRazorContext _context;

        public EditModel(FitRazorContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string EntityName { get; set; } = "Trainers";

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public bool EntityNotFound { get; set; } = false;

        // GET: Загрузка данных для формы
        public async Task<IActionResult> OnGetAsync()
        {
            var exists = await EntityExistsAsync();

            if (!exists)
            {
                EntityNotFound = true;
                return Page();
            }

            return Page();
        }

        // POST: Сохранение изменений
        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var exists = await EntityExistsAsync();

                if (!exists)
                {
                    TempData["ErrorMessage"] = "Запись не найдена";
                    return RedirectToPage("Index", new { entityName = EntityName });
                }

                // Вызываем метод обновления для конкретной сущности
                switch (EntityName)
                {
                    case "Trainers":
                        await UpdateTrainerAsync();
                        break;
                    case "Clients":
                        await UpdateClientAsync();
                        break;
                    case "Services":
                        await UpdateServiceAsync();
                        break;
                    case "Bookings":
                        await UpdateBookingAsync();
                        break;
                    case "TrainerServices":
                        await UpdateTrainerServiceAsync();
                        break;
                    default:
                        throw new ArgumentException($"Неизвестная сущность: {EntityName}");
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Запись успешно обновлена!";
                return RedirectToPage("Index", new { entityName = EntityName });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при обновлении: {ex.Message}";
                return RedirectToPage("Edit", new { entityName = EntityName, id = Id });
            }
        }

        private async Task<bool> EntityExistsAsync()
        {
            return EntityName switch
            {
                "Trainers" => await _context.Trainers.AnyAsync(t => t.TrainerId == Id),
                "Clients" => await _context.Clients.AnyAsync(c => c.ClientId == Id),
                "Services" => await _context.Services.AnyAsync(s => s.ServiceId == Id),
                "Bookings" => await _context.Bookings.AnyAsync(b => b.BookingId == Id),
                "TrainerServices" => await _context.TrainerServices.AnyAsync(ts => ts.TrainerServiceId == Id),
                _ => false
            };
        }

        private async Task UpdateTrainerAsync()
        {
            var trainer = await _context.Trainers.FindAsync(Id);
            if (trainer == null) return;

            trainer.FullName = Request.Form["FullName"];
            trainer.Phone = Request.Form["Phone"];
            trainer.Email = Request.Form["Email"];
            trainer.Slogan = Request.Form["Slogan"];
            trainer.Specialization = Request.Form["Specialization"];
            trainer.SpecializationDescription = Request.Form["SpecializationDescription"];
            trainer.Motto = Request.Form["Motto"];
            trainer.Education = Request.Form["Education"];
            trainer.WorkExperience = Request.Form["WorkExperience"];
            trainer.SportsAchievements = Request.Form["SportsAchievements"];
            trainer.Salary = decimal.Parse(Request.Form["Salary"]);
            trainer.PhotoUrl = Request.Form["PhotoUrl"];
        }

        private async Task UpdateClientAsync()
        {
            var client = await _context.Clients.FindAsync(Id);
            if (client == null) return;

            client.FullName = Request.Form["FullName"];
            client.Phone = Request.Form["Phone"];
            client.Email = Request.Form["Email"];
            client.BirthDate = string.IsNullOrEmpty(Request.Form["BirthDate"])
                ? null
                : DateOnly.Parse(Request.Form["BirthDate"]);
            // RegistrationDate не меняем - это дата создания
        }

        private async Task UpdateServiceAsync()
        {
            var service = await _context.Services.FindAsync(Id);
            if (service == null) return;

            service.ServiceName = Request.Form["ServiceName"];
            service.DurationMinutes = int.Parse(Request.Form["DurationMinutes"]);
            service.BasePrice = decimal.Parse(Request.Form["BasePrice"]);
            service.Description = Request.Form["Description"];
        }

        private async Task UpdateBookingAsync()
        {
            var booking = await _context.Bookings.FindAsync(Id);
            if (booking == null) return;

            booking.ClientId = int.Parse(Request.Form["ClientId"]);
            booking.TrainerServiceId = int.Parse(Request.Form["TrainerServiceId"]);
            booking.BookingDateTime = DateTime.Parse(Request.Form["BookingDateTime"]);
            booking.SessionsCount = int.Parse(Request.Form["SessionsCount"]);
            booking.UnitPrice = decimal.Parse(Request.Form["UnitPrice"]);
            booking.TotalPrice = booking.UnitPrice * booking.SessionsCount;
            booking.Status = Request.Form["Status"];
            booking.Notes = Request.Form["Notes"];
            // CreatedDate не меняем
        }

        private async Task UpdateTrainerServiceAsync()
        {
            var trainerService = await _context.TrainerServices.FindAsync(Id);
            if (trainerService == null) return;

            trainerService.TrainerId = int.Parse(Request.Form["TrainerId"]);
            trainerService.ServiceId = int.Parse(Request.Form["ServiceId"]);
        }
    }
}
