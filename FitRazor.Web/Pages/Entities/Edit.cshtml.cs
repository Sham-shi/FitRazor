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
        private readonly IWebHostEnvironment _env;

        public EditModel(FitRazorContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty(SupportsGet = true)] public string EntityName { get; set; } = "Trainers";
        [BindProperty(SupportsGet = true)] public int Id { get; set; }
        public bool EntityNotFound { get; set; }

        // Для фото (Trainer.PhotoUrl)
        [BindProperty] public IFormFile? PhotoUrl { get; set; }          // имя должно совпадать с input name
        [BindProperty] public string? OldPhotoUrl { get; set; }          // старый путь

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

                if (EntityName == "Trainers" && entity is Trainer trainer)
                {
                    string? newPhotoPath = null;

                    try
                    {
                        newPhotoPath = await Helper.SaveImageAsync(
                            file: PhotoUrl,
                            env: _env,
                            subfolder: "Trainers",
                            oldPath: OldPhotoUrl
                        );
                    }
                    catch (ArgumentException ex)
                    {
                        TempData["ErrorMessage"] = ex.Message;
                        return RedirectToPage("Edit", new { entityName = EntityName, id = Id });
                    }

                    trainer.PhotoUrl = newPhotoPath ?? OldPhotoUrl;

                    // Если загрузили новое → обновляем, иначе оставляем старое
                    trainer.PhotoUrl = newPhotoPath ?? OldPhotoUrl;
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
