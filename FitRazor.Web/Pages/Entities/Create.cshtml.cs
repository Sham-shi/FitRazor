using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitRazor.Web.Pages.Entities
{
    [BindProperties]
    public class CreateModel : PageModel
    {
        private readonly FitRazorContext _context;
        private readonly IWebHostEnvironment _env;

        public CreateModel(FitRazorContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [BindProperty(SupportsGet = true)] public string EntityName { get; set; } = "Trainers";

        // Для фото (Trainer.PhotoUrl)
        [BindProperty] public IFormFile? PhotoUrl { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                TempData["ErrorMessage"] = "Неизвестная сущность";
                return RedirectToPage("Create", new { entityName = EntityName });
            }

            try
            {
                // Создаём новый экземпляр
                var entity = Activator.CreateInstance(meta.EntityType);
                if (entity == null)
                {
                    throw new InvalidOperationException("Не удалось создать экземпляр сущности");
                }

                // Применяем значения из формы
                Helper.ApplyFormValuesToEntity(entity, Request.Form);

                // Специальная обработка фото (для Trainers)
                if (EntityName == "Trainers" && entity is Trainer trainer && PhotoUrl != null && PhotoUrl.Length > 0)
                {
                    try
                    {
                        var newPhotoPath = await Helper.SaveImageAsync(
                            file: PhotoUrl,
                            env: _env,
                            subfolder: "Trainers"
                        // oldPath не передаём → null
                        );

                        if (newPhotoPath != null)
                        {
                            trainer.PhotoUrl = newPhotoPath;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        TempData["ErrorMessage"] = ex.Message;
                        return Page();
                    }
                }

                // Специальная логика для отдельных сущностей
                switch (EntityName) // ← этот switch можно тоже вынести в реестр позже
                {
                    case "Clients" when entity is Client client:
                        client.RegistrationDate = DateOnly.FromDateTime(DateTime.Today);
                        break;

                    case "Bookings" when entity is Booking booking:
                        booking.TotalPrice = booking.UnitPrice * booking.SessionsCount;
                        booking.CreatedDate = DateTime.Now;
                        break;
                }

                // Добавляем в контекст
                await _context.AddAsync(entity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Запись успешно создана!";
                return RedirectToPage("Index", new { entityName = EntityName });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при создании: {ex.Message}";
                return RedirectToPage("Create", new { entityName = EntityName });
            }
        }
    }
}