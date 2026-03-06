using FitRazor.Data.Models;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities
{
    //[Authorize(Roles = "Trainer,Admin")]
    public class IndexModel : PageModel
    {
        private readonly FitRazorContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(FitRazorContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public string EntityName { get; set; } = "Trainers";

        public void OnGet(string entityName)
        {
            EntityName = entityName ?? "Trainers";
            _logger.LogDebug("Запрос списка сущности: {EntityName}", EntityName);

            // Валидация имени сущности
            if (EntityAdminRegistry.Get(EntityName) == null)
            {
                _logger.LogWarning("Получено неизвестное имя сущности: {EntityName}, используем значение по умолчанию", EntityName);
                EntityName = "Trainers"; // или ошибка
            }
            else
            {
                _logger.LogDebug("Сущность {EntityName} валидна, продолжаем загрузку", EntityName);
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string entityName, int id)
        {
            _logger.LogInformation("Запрос на удаление {EntityName}#{Id}", entityName, id);

            try
            {
                var meta = EntityAdminRegistry.Get(entityName);
                if (meta == null)
                {
                    _logger.LogWarning("Попытка удаления неизвестной сущности: {EntityName}", entityName);
                    TempData["ErrorMessage"] = "Неизвестная сущность";
                    return RedirectToPage("Index", new { entityName });
                }

                _logger.LogDebug("Выполняем удаление через мета-сервис для {EntityName}#{Id}", entityName, id);
                var (success, error) = await meta.DeleteAsync(_context, id);

                if (success)
                {
                    _logger.LogInformation("Успешно удалена запись {EntityName}#{Id}", entityName, id);
                    TempData["SuccessMessage"] = "Запись успешно удалена!";
                }
                else
                {
                    _logger.LogWarning("Не удалось удалить {EntityName}#{Id}: {Error}", entityName, id, error ?? "Запись не найдена");
                    TempData["ErrorMessage"] = error ?? "Запись не найдена или уже удалена";
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
            {
                // Специфичная обработка ошибок внешних ключей
                _logger.LogWarning(ex, "Не удалось удалить {EntityName}#{Id}: запись используется в других таблицах (нарушение внешнего ключа)", entityName, id);
                TempData["ErrorMessage"] = "Невозможно удалить: запись используется в других данных";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при удалении {EntityName}#{Id}", entityName, id);
                TempData["ErrorMessage"] = $"Ошибка при удалении: {ex.Message}";
            }

            return RedirectToPage("Index", new { entityName });
        }
    }
}
