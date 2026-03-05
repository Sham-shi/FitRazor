using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities
{
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly FitRazorContext _context;

        public EditModel(FitRazorContext context) => _context = context;

        [BindProperty(SupportsGet = true)] public string EntityName { get; set; } = "Trainers";
        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        public bool EntityNotFound { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                EntityNotFound = true;
                return Page();
            }

            var exists = await meta.ExistsAsync(_context, Id);
            if (!exists)
            {
                EntityNotFound = true;
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                TempData["ErrorMessage"] = "Неизвестная сущность";
                return RedirectToPage("Index", new { entityName = EntityName });
            }

            var exists = await meta.ExistsAsync(_context, Id);
            if (!exists)
            {
                TempData["ErrorMessage"] = "Запись не найдена";
                return RedirectToPage("Index", new { entityName = EntityName });
            }

            try
            {
                // Загружаем существующую сущность
                var entity = await meta.GetByIdAsync(_context, Id);
                if (entity == null)
                {
                    TempData["ErrorMessage"] = "Запись не найдена";
                    return RedirectToPage("Index", new { entityName = EntityName });
                }

                // Применяем значения из формы
                Helper.ApplyFormValuesToEntity(entity, Request.Form);

                // Специальная логика для Booking (TotalPrice)
                if (EntityName == "Bookings" && entity is Booking booking)
                {
                    booking.TotalPrice = booking.UnitPrice * booking.SessionsCount;
                }

                // CreatedDate / RegistrationDate обычно не меняем — они уже защищены

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
    }
}
