using FitRazor.Data.Models;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-card")]
    public class EntityCardTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("entity-id")]
        public int EntityId { get; set; }

        public EntityCardTagHelper(FitRazorContext context)
        {
            _context = context;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var entity = await GetEntityAsync();

            output.TagName = "div";
            output.Attributes.SetAttribute("class", "card shadow-sm");

            if (entity == null)
            {
                output.Content.SetHtmlContent("<div class='card-body'><div class='alert alert-warning'>Запись не найдена</div></div>");
                return;
            }

            var html = new System.Text.StringBuilder();
            html.Append("<div class='card-body'>");

            foreach (var prop in entity.GetType().GetProperties()
                .Where(p =>
                    p.CanRead &&
                    !p.PropertyType.IsGenericType &&
                    !p.PropertyType.IsCollection() && // Убедитесь, что extension method доступен в этом файле
                    p.Name != "Id")                 // Исключаем стандартный PK
                .OrderBy(p =>
                {
                    var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
                    return displayAttr?.GetOrder() ?? 1000;
                }))
            {
                var value = prop.GetValue(entity);
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;

                if (value != null)
                {
                    html.Append("<div class='row mb-2'>");
                    html.Append($"<div class='col-md-4 fw-bold'>{displayName}:</div>");
                    html.Append("<div class='col-md-8'>");
                    html.Append(FormatValue(value, prop.PropertyType));
                    html.Append("</div></div>");
                }
            }

            html.Append("</div>");
            output.Content.SetHtmlContent(html.ToString());
        }

        private async Task<object?> GetEntityAsync()
        {
            return EntityName switch
            {
                "Trainers" => await _context.Trainers.FindAsync(EntityId),
                "Clients" => await _context.Clients.FindAsync(EntityId),
                "Services" => await _context.Services.FindAsync(EntityId),
                "Bookings" => await _context.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.TrainerService)
                    .FirstOrDefaultAsync(b => b.BookingId == EntityId),
                "TrainerServices" => await _context.TrainerServices
                    .Include(ts => ts.Trainer)
                    .Include(ts => ts.Service)
                    .FirstOrDefaultAsync(ts => ts.TrainerServiceId == EntityId),
                _ => null
            };
        }

        private string FormatValue(object value, Type type)
        {
            if (type == typeof(decimal)) return $"<span class='text-success fw-bold'>{((decimal)value):N2} ₽</span>";
            if (type == typeof(DateTime)) return $"<span>{((DateTime)value):dd.MM.yyyy HH:mm}</span>";
            if (type == typeof(DateOnly)) return $"<span>{((DateOnly)value):dd.MM.yyyy}</span>";
            return value.ToString() ?? "—";
        }
    }
}