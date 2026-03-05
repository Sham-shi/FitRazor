using FitRazor.Data.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("delete-modal")]
    public class DeleteModalTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("entity-id")]
        public int EntityId { get; set; }

        [HtmlAttributeName("entity-name-display")]
        public string EntityNameDisplay { get; set; } = "";

        [HtmlAttributeName("modal-id")]
        public string ModalId { get; set; } = "deleteModal";

        public DeleteModalTagHelper(FitRazorContext context)
        {
            _context = context;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var displayName = await GetDisplayNameAsync();

            output.TagName = "div";
            output.Attributes.SetAttribute("class", "modal fade");
            output.Attributes.SetAttribute("id", ModalId);
            output.Attributes.SetAttribute("tabindex", "-1");
            output.Attributes.SetAttribute("aria-labelledby", $"{ModalId}Label");
            output.Attributes.SetAttribute("aria-hidden", "true");
            output.Attributes.SetAttribute("data-bs-backdrop", "static");

            var html = GenerateHtml(displayName);
            output.Content.SetHtmlContent(html);
        }

        private async Task<string> GetDisplayNameAsync()
        {
            if (!string.IsNullOrEmpty(EntityNameDisplay))
            {
                return EntityNameDisplay;
            }

            return EntityName switch
            {
                "Trainers" => await GetTrainerNameAsync(),
                "Clients" => await GetClientNameAsync(),
                "Services" => await GetServiceNameAsync(),
                "Bookings" => await GetBookingNameAsync(),
                "TrainerServices" => await GetTrainerServiceNameAsync(),
                _ => $"Запись #{EntityId}"
            };
        }

        private async Task<string> GetTrainerNameAsync()
        {
            var trainer = await _context.Trainers.FindAsync(EntityId);
            return trainer?.FullName ?? $"Тренер #{EntityId}";
        }

        private async Task<string> GetClientNameAsync()
        {
            var client = await _context.Clients.FindAsync(EntityId);
            return client?.FullName ?? $"Клиент #{EntityId}";
        }

        private async Task<string> GetServiceNameAsync()
        {
            var service = await _context.Services.FindAsync(EntityId);
            return service?.ServiceName ?? $"Услуга #{EntityId}";
        }

        private async Task<string> GetBookingNameAsync()
        {
            var booking = await _context.Bookings
                .Include(b => b.Client)
                .Include(b => b.TrainerService)
                .FirstOrDefaultAsync(b => b.BookingId == EntityId);

            if (booking == null) return $"Бронирование #{EntityId}";

            var trainerName = booking.TrainerService?.Trainer?.FullName ?? "Тренер";
            var serviceName = booking.TrainerService?.Service?.ServiceName ?? "Услуга";
            return $"{booking.Client?.FullName} — {serviceName} ({trainerName})";
        }

        private async Task<string> GetTrainerServiceNameAsync()
        {
            var ts = await _context.TrainerServices
                .Include(ts => ts.Trainer)
                .Include(ts => ts.Service)
                .FirstOrDefaultAsync(ts => ts.TrainerServiceId == EntityId);

            if (ts == null) return $"Запись #{EntityId}";

            return $"{ts.Trainer?.FullName} — {ts.Service?.ServiceName}";
        }

        private string GenerateHtml(string displayName)
        {
            return $@"
<div class=""modal-dialog modal-dialog-centered"">
        <div class=""modal-content"">
            <div class=""modal-header"">
                <h5 class=""modal-title"" id=""{ModalId}Label"">
                    ⚠️ Подтверждение удаления
                </h5>
                <button type=""button"" class=""btn-close"" data-bs-dismiss=""modal"" aria-label=""Закрыть""></button>
            </div>
            <div class=""modal-body"">
                <p>Вы уверены, что хотите удалить запись?</p>
                <p class=""text-muted small""><strong>{displayName}</strong></p>
                <div class=""alert alert-warning mb-0"">
                    <i class=""bi bi-exclamation-triangle""></i>
                    Это действие нельзя отменить!
                </div>
            </div>
            <div class=""modal-footer"">
                <form method=""post"" asp-page=""/Entities/Index"" asp-page-handler=""Delete"" asp-route-entityName=""{EntityName}"" asp-route-id=""{EntityId}"">
                    <button type=""submit"" class=""btn btn-danger"">
                        🗑️ Да, удалить
                    </button>
                    <button type=""button"" class=""btn btn-secondary"" data-bs-dismiss=""modal"">
                        ❌ Отмена
                    </button>
                </form>
            </div>
        </div>
</div>";
        }
    }
}