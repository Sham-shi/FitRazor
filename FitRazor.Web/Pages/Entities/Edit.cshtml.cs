using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities
{
    [Authorize(Roles = "Trainer,Admin")]
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly FitRazorContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EditModel> _logger;

        public EditModel(FitRazorContext context, IWebHostEnvironment env, ILogger<EditModel> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
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
                _logger.LogWarning("Попытка редактирования неизвестной сущности: {EntityName}", EntityName);
                EntityNotFound = true;
                return Page();
            }

            var exists = await meta.ExistsAsync(_context, Id);
            if (!exists)
            {
                _logger.LogWarning("Запись {EntityName}#{Id} не найдена в БД", EntityName, Id);
                EntityNotFound = true;
                return Page();
            }

            _logger.LogDebug("Запись {EntityName}#{Id} найдена, отображаем форму", EntityName, Id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Запрос на редактирование {EntityName}#{Id}", EntityName, Id);

            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                _logger.LogWarning("Попытка обновления неизвестной сущности: {EntityName}", EntityName);
                TempData["ErrorMessage"] = "Неизвестная сущность";
                return RedirectToPage("Index", new { entityName = EntityName });
            }

            var exists = await meta.ExistsAsync(_context, Id);
            if (!exists)
            {
                _logger.LogWarning("Попытка обновить несуществующую запись {EntityName}#{Id}", EntityName, Id);
                TempData["ErrorMessage"] = "Запись не найдена";
                return RedirectToPage("Index", new { entityName = EntityName });
            }

            try
            {
                // Загружаем существующую сущность
                var entity = await meta.GetByIdAsync(_context, Id);
                if (entity == null)
                {
                    _logger.LogError("Не удалось загрузить сущность {EntityName}#{Id} из БД", EntityName, Id);
                    TempData["ErrorMessage"] = "Запись не найдена";
                    return RedirectToPage("Index", new { entityName = EntityName });
                }

                // Применяем значения из формы
                _logger.LogDebug("Применяем данные формы к сущности {EntityName}#{Id}", EntityName, Id);
                Helper.ApplyFormValuesToEntity(entity, Request.Form);

                // Специальная логика для Booking (TotalPrice)
                if (EntityName == "Bookings" && entity is Booking booking)
                {
                    var oldPrice = booking.TotalPrice;
                    booking.TotalPrice = booking.UnitPrice * booking.SessionsCount;
                    _logger.LogDebug("Пересчитана стоимость бронирования {BookingId}: {OldPrice} → {NewPrice}",
                        Id, oldPrice, booking.TotalPrice);
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

                        if (newPhotoPath != null)
                        {
                            _logger.LogInformation("Обновлено фото тренера #{Id}: {OldPath} → {NewPath}",
                                Id, OldPhotoUrl ?? "null", newPhotoPath);
                        }
                        else if (PhotoUrl != null && PhotoUrl.Length == 0)
                        {
                            _logger.LogDebug("Пользователь не выбрал файл, оставляем старое фото: {OldPath}", OldPhotoUrl);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Неверный формат фото для тренера #{Id}", Id);
                        TempData["ErrorMessage"] = ex.Message;
                        return RedirectToPage("Edit", new { entityName = EntityName, id = Id });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке фото для тренера #{Id}", Id);
                        throw; // пробрасываем в общий catch
                    }

                    trainer.PhotoUrl = newPhotoPath ?? OldPhotoUrl;

                    // Если загрузили новое → обновляем, иначе оставляем старое
                    trainer.PhotoUrl = newPhotoPath ?? OldPhotoUrl;
                }

                _logger.LogDebug("Сохраняем изменения в БД для {EntityName}#{Id}", EntityName, Id);
                var saveResult = await _context.SaveChangesAsync();

                if (saveResult > 0)
                {
                    _logger.LogInformation("Успешно обновлена запись {EntityName}#{Id}, затронуто строк: {SaveCount}",
                        EntityName, Id, saveResult);

                    TempData["SuccessMessage"] = "Запись успешно обновлена!";
                    return RedirectToPage("Index", new { entityName = EntityName });
                }
                else
                {
                    _logger.LogWarning("SaveChangesAsync вернул 0 при обновлении {EntityName}#{Id} — возможно, данные не изменились",
                        EntityName, Id);

                    TempData["SuccessMessage"] = "Запись обновлена (изменения не потребовались)";
                    return RedirectToPage("Index", new { entityName = EntityName });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при обновлении {EntityName}#{Id}. Контекст: {@Context}",
                    EntityName, Id,
                    new
                    {
                        FormKeys = Request.Form.Keys,
                        HasFile = PhotoUrl != null,
                        FileName = PhotoUrl?.FileName,
                        UserAgent = Request.Headers.UserAgent.ToString()
                    });

                TempData["ErrorMessage"] = $"Ошибка при обновлении: {ex.Message}";
                return RedirectToPage("Edit", new { entityName = EntityName, id = Id });
            }
        }
    }
}
