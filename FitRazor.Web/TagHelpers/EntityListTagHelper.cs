using FitRazor.Data.Models;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-list")]
    public class EntityListTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;

        // Название сущности (Trainers, Clients, Services...)
        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        // Заголовок таблицы
        [HtmlAttributeName("table-title")]
        public string? TableTitle { get; set; }

        // Показывать кнопки действий (Edit, Delete)
        [HtmlAttributeName("show-actions")]
        public bool ShowActions { get; set; } = true;

        // Страница для деталей
        [HtmlAttributeName("details-page")]
        public string? DetailsPage { get; set; } = "/Entities/Details";

        // Страница для редактирования
        [HtmlAttributeName("edit-page")]
        public string? EditPage { get; set; } = "/Entities/Edit";

        public EntityListTagHelper(FitRazorContext context)
        {
            _context = context;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Получаем данные
            var data = await GetDataAsync();

            // Генерируем HTML
            output.TagName = "div";
            output.Attributes.SetAttribute("class", "entity-list-container");

            var html = GenerateHtml(data);
            output.Content.SetHtmlContent(html);
        }

        private async Task<IEnumerable<object>> GetDataAsync()
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null) return Enumerable.Empty<object>();
            var query = meta.QueryFactory(_context);
            return await query.ToListAsync();
        }

        private string GenerateHtml(IEnumerable<object> data)
        {
            var items = data.ToList();
            if (!items.Any())
            {
                return "<div class='alert alert-info'>Записей не найдено</div>";
            }

            var firstItem = items.First();
            var properties = GetDisplayableProperties(firstItem.GetType());

            var html = new System.Text.StringBuilder();

            // Заголовок
            if (!string.IsNullOrEmpty(TableTitle))
            {
                html.Append($"<h2 class='mb-3'>{TableTitle}</h2>");
            }

            // Таблица
            html.Append("<div class='table-responsive'>");
            html.Append("<table class='table table-hover table-bordered'>");

            // Заголовки колонок
            html.Append("<thead class='table-light'>");
            html.Append("<tr>");
            html.Append("<th class='text-center' style='width: 50px;'>#</th>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
                html.Append($"<th>{displayName}</th>");
            }

            if (ShowActions)
            {
                html.Append("<th class='text-center' style='width: 200px;'>Действия</th>");
            }

            html.Append("</tr>");
            html.Append("</thead>");

            // Тело таблицы
            html.Append("<tbody>");
            int index = 1;
            foreach (var item in items)
            {
                html.Append("<tr>");
                html.Append($"<td class='text-center'>{index}</td>");

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    html.Append("<td>");

                    if (value != null)
                    {
                        // Простое форматирование по типу
                        html.Append(FormatValue(value, prop.PropertyType));
                    }
                    else
                    {
                        html.Append("<span class='text-muted'>—</span>");
                    }

                    html.Append("</td>");
                }

                if (ShowActions)
                {
                    var id = GetIdValue(item);
                    var displayName = GetDisplayNameForModal(item, EntityName);

                    html.Append("<td class='text-center'>");
                    html.Append($"<a href='{DetailsPage}/{EntityName}/{id}' class='btn btn-sm btn-info me-1'>📄</a>");
                    html.Append($"<a href='{EditPage}/{EntityName}/{id}' class='btn btn-sm btn-primary me-1'>✏️</a>");
                    html.Append($@"
                        <button type='button' class='btn btn-sm btn-danger'
                                data-bs-toggle='modal' data-bs-target='#deleteModal'
                                data-entity-name='{EntityName}'
                                data-entity-id='{id}'
                                data-entity-display='{System.Web.HttpUtility.HtmlAttributeEncode(displayName)}'>
                            🗑️
                        </button>");
                    html.Append("</td>");
                }

                html.Append("</tr>");
                index++;
            }
            html.Append("</tbody>");
            html.Append("</table>");
            html.Append("</div>");

            return html.ToString();
        }

        // Метод для получения имени (без запроса к БД!)
        private string GetDisplayNameForModal(object item, string entityName)
        {
            // Пытаемся получить имя через рефлексию
            var nameProp = item.GetType().GetProperty("FullName")
                        ?? item.GetType().GetProperty("Name")
                        ?? item.GetType().GetProperty("ServiceName");
            return nameProp?.GetValue(item)?.ToString() ?? $"{entityName} #{GetIdValue(item)}";
        }

        private IEnumerable<PropertyInfo> GetDisplayableProperties(Type type)
        {
            return type.GetProperties()
                .Where(p =>
                    p.CanRead &&
                    !p.PropertyType.IsGenericType &&
                    (p.PropertyType.Namespace == "System" ||
                     p.PropertyType == typeof(string) ||
                     p.PropertyType == typeof(decimal) ||
                     p.PropertyType == typeof(DateTime) ||
                     p.PropertyType == typeof(DateOnly) ||
                     p.PropertyType == typeof(int)));
        }

        private string FormatValue(object value, Type type)
        {
            if (type == typeof(decimal))
            {
                return $"<span class='text-success fw-bold'>{((decimal)value):N2} ₽</span>";
            }
            if (type == typeof(DateTime))
            {
                return $"<span>{((DateTime)value):dd.MM.yyyy HH:mm}</span>";
            }
            if (type == typeof(DateOnly))
            {
                return $"<span>{((DateOnly)value):dd.MM.yyyy}</span>";
            }
            if (type == typeof(string))
            {
                var str = value.ToString();
                return string.IsNullOrEmpty(str) ? "<span class='text-muted'>—</span>" : str;
            }
            return value.ToString() ?? "—";
        }

        private object GetIdValue(object item)
        {
            var idProp = item.GetType().GetProperties()
                .FirstOrDefault(p => p.Name.EndsWith("Id") || p.Name == "Id");
            return idProp?.GetValue(item) ?? "0";
        }
    }
}