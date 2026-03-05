using FitRazor.Data.Models;
using FitRazor.Web.Services.Admin;
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
            if (EntityAdminRegistry.Get(EntityName) == null)
            {
                EntityName = "Trainers"; // или ошибка
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string entityName, int id)
        {
            try
            {
                var meta = EntityAdminRegistry.Get(entityName);
                if (meta == null)
                {
                    TempData["ErrorMessage"] = "Неизвестная сущность";
                    return RedirectToPage("Index", new { entityName });
                }

                var (success, error) = await meta.DeleteAsync(_context, id);

                if (success)
                {
                    TempData["SuccessMessage"] = "Запись успешно удалена!";
                }
                else
                {
                    TempData["ErrorMessage"] = error ?? "Запись не найдена или уже удалена";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка при удалении: {ex.Message}";
            }

            return RedirectToPage("Index", new { entityName });
        }
    }
}
