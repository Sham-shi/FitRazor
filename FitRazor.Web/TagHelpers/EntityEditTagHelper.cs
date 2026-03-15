using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-edit-form")]
    public class EntityEditTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("entity-id")]
        public int EntityId { get; set; }

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Сохранить";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; } = "/Entities/Index";

        public EntityEditTagHelper(FitRazorContext context, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                output.TagName = "div";
                output.Attributes.SetAttribute("class", "alert alert-warning");
                output.Content.SetHtmlContent($"⚠️ Метаданные для '{EntityName}' не найдены");
                return;
            }

            var entity = await meta.GetByIdAsync(_context, EntityId);
            if (entity == null)
            {
                output.TagName = "div";
                output.Attributes.SetAttribute("class", "alert alert-warning");
                output.Content.SetHtmlContent("⚠️ Запись не найдена");
                return;
            }

            var modelType = entity.GetType();
            var properties = meta.GetEditPropertiesFunc?.Invoke(modelType)
                           ?? Helper.GetFormProperties(modelType);

            // Фильтруем скрытые свойства
            properties = properties.Where(p => !meta.HiddenProperties.Contains(p.Name));

            // Загружаем данные для выпадающих списков
            var dropdownData = new Dictionary<string, IEnumerable<SelectListItem>>();
            foreach (var provider in meta.DropdownProviders)
            {
                dropdownData[provider.Key] = await provider.Value(_context);
            }

            output.TagName = "form";
            output.Attributes.SetAttribute("method", "post");
            output.Attributes.SetAttribute("enctype", "multipart/form-data");
            output.Attributes.SetAttribute("class", "entity-edit-form");

            var html = GenerateHtml(entity, modelType, properties, dropdownData, meta);
            output.Content.SetHtmlContent(html);
        }

        private string GenerateHtml(object entity, Type modelType, IEnumerable<PropertyInfo> properties,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData, EntityAdminMetadata meta)
        {
            var html = new StringBuilder();

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.RequestServices?.GetService<IAntiforgery>() is IAntiforgery antiforgery)
            {
                var tokens = antiforgery.GetAndStoreTokens(httpContext);
                html.Append($"<input name='__RequestVerificationToken' type='hidden' value='{tokens.RequestToken}' />");
            }

            // 🔹 Скрытое поле с ID (нужно для EF Core)
            var idProp = modelType.GetProperty(meta.KeyPropertyName);
            if (idProp != null)
            {
                var idValue = idProp.GetValue(entity)?.ToString() ?? "0";
                html.Append($"<input type='hidden' name='{meta.KeyPropertyName}' value='{idValue}' />");
            }

            html.Append("<div class='row'>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var currentValue = prop.GetValue(entity);
                var propName = prop.Name;

                html.Append("<div class='col-md-6 mb-3'>");
                html.Append($"<label class='form-label fw-semibold'>");
                html.Append(displayName);
                if (isRequired) html.Append(" <span class='text-danger'>*</span>");
                html.Append("</label>");

                // 🔹 Проверяем кастомный генератор инпута
                if (meta.CustomInputGenerators?.TryGetValue(propName, out var generator) == true && generator != null)
                {
                    html.Append(generator(prop, currentValue, displayName, dropdownData));
                }
                else
                {
                    // 🔹 Стандартная генерация инпута
                    html.Append(GenerateStandardInput(prop, currentValue, dropdownData, meta));
                }

                // Валидация (для Razor Pages)
                html.Append($"<span asp-validation-for='{propName}' class='text-danger small'></span>");
                html.Append("</div>");
            }

            html.Append("</div>");

            // 🔹 Кнопки
            html.Append("<div class='row mt-4 pt-3 border-top'>");
            html.Append("<div class='col-12 d-flex gap-2'>");
            html.Append($"<button type='submit' class='btn btn-primary px-4'><i class='bi bi-save me-2'></i>{SubmitText}</button>");
            html.Append($"<a href='{CancelPage}/{EntityName}' class='btn btn-outline-secondary'><i class='bi bi-x-lg me-2'></i>Отмена</a>");
            html.Append("</div>");
            html.Append("</div>");

            return html.ToString();
        }

        private string GenerateStandardInput(PropertyInfo prop, object? currentValue,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData, EntityAdminMetadata meta)
        {
            var propType = prop.PropertyType;
            var propName = prop.Name;
            var isReadOnly = meta.ReadOnlyProperties.Contains(propName);
            var readOnlyAttr = isReadOnly ? "readonly" : "";
            var classAttr = isReadOnly ? "form-control bg-light" : "form-control";

            // 🔹 Обработка фото (если есть конфиг)
            if (meta.PhotoUploadConfigs.TryGetValue(propName, out var photoConfig) ||
                propName.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                propName.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                propName.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase))
            {
                var config = photoConfig ?? new PhotoUploadConfig { Subfolder = "Uploads" };
                return GeneratePhotoInput(prop, currentValue, propName, config);
            }

            // 🔹 Выпадающий список для FK
            if (propName.EndsWith("Id") && dropdownData.ContainsKey(propName))
            {
                return GenerateSelectInput(propName, currentValue, dropdownData[propName], isReadOnly);
            }

            // 🔹 Текстовые поля
            if (propType == typeof(string))
            {
                return GenerateTextInput(prop, currentValue?.ToString() ?? "", classAttr, readOnlyAttr);
            }

            // 🔹 Числовые поля
            if (propType == typeof(int) || propType == typeof(int?))
            {
                var value = currentValue?.ToString() ?? "0";
                return $"<input type='number' name='{propName}' class='{classAttr}' value='{value}' {readOnlyAttr} />";
            }

            if (propType == typeof(decimal) || propType == typeof(decimal?))
            {
                var value = currentValue is decimal d
                    ? d.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
                    : "0.00";
                return $"<input type='number' name='{propName}' class='{classAttr}' value='{value}' step='0.01' {readOnlyAttr} />";
            }

            // 🔹 Дата и время
            if (propType == typeof(DateTime) || propType == typeof(DateTime?))
            {
                var value = currentValue != null
                    ? ((DateTime)currentValue).ToString("yyyy-MM-ddTHH:mm")
                    : "";
                return $"<input type='datetime-local' name='{propName}' class='{classAttr}' value='{value}' {readOnlyAttr} />";
            }

            if (propType == typeof(DateOnly) || propType == typeof(DateOnly?))
            {
                var value = currentValue != null
                    ? ((DateOnly)currentValue).ToString("yyyy-MM-dd")
                    : "";
                return $"<input type='date' name='{propName}' class='{classAttr}' value='{value}' {readOnlyAttr} />";
            }

            // 🔹 По умолчанию
            var defaultValue = currentValue?.ToString() ?? "";
            return $"<input type='text' name='{propName}' class='{classAttr}' value='{defaultValue}' {readOnlyAttr} />";
        }

        private string GenerateTextInput(PropertyInfo prop, string value, string classAttr, string readOnlyAttr)
        {
            var propName = prop.Name;
            var maxLength = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? 500;
            var isEmail = propName.Contains("Email", StringComparison.OrdinalIgnoreCase);
            var isPhone = propName.Contains("Phone", StringComparison.OrdinalIgnoreCase);
            var isUrl = propName.Contains("Url", StringComparison.OrdinalIgnoreCase) && !propName.Contains("Photo", StringComparison.OrdinalIgnoreCase);

            if (isEmail)
                return $"<input type='email' name='{propName}' class='{classAttr}' value='{value}' maxlength='{maxLength}' {readOnlyAttr} />";
            if (isPhone)
                return $"<input type='tel' name='{propName}' class='{classAttr}' value='{value}' maxlength='{maxLength}' {readOnlyAttr} placeholder='+7 (___) ___-__-__' />";
            if (isUrl)
                return $"<input type='url' name='{propName}' class='{classAttr}' value='{value}' maxlength='{maxLength}' {readOnlyAttr} />";

            // Многострочное поле для длинного текста
            if (maxLength > 200)
                return $"<textarea name='{propName}' class='{classAttr}' rows='3' maxlength='{maxLength}' {readOnlyAttr}>{value}</textarea>";

            return $"<input type='text' name='{propName}' class='{classAttr}' value='{value}' maxlength='{maxLength}' {readOnlyAttr} />";
        }

        private string GenerateSelectInput(string propName, object? currentValue,
            IEnumerable<SelectListItem> options, bool isReadOnly)
        {
            var sb = new StringBuilder();
            var disabledAttr = isReadOnly ? "disabled" : "";
            var hiddenInput = isReadOnly ? $"<input type='hidden' name='{propName}' value='{currentValue}' />" : "";

            sb.Append($"<select name='{propName}' class='form-select' {disabledAttr}>");
            sb.Append("<option value=''>— Выберите —</option>");
            foreach (var opt in options)
            {
                var selected = currentValue?.ToString() == opt.Value ? "selected" : "";
                sb.Append($"<option value='{opt.Value}' {selected}>{opt.Text}</option>");
            }
            sb.Append("</select>");
            sb.Append(hiddenInput); // для отправки значения при readonly
            return sb.ToString();
        }

        public static string GeneratePhotoInput(PropertyInfo prop, object? currentValue,
            string fieldName, PhotoUploadConfig config)
        {
            var currentUrl = currentValue?.ToString() ?? "";
            var displayUrl = string.IsNullOrWhiteSpace(currentUrl)
                ? config.DefaultImagePath
                : (currentUrl.StartsWith("http") ? currentUrl : "/" + currentUrl.TrimStart('~', '/'));

            var sb = new StringBuilder();

            // 🔹 Превью текущего фото
            sb.Append("<div class='current-photo mb-2'>");
            sb.Append($"<label class='form-label small'>{config.PreviewLabel}:</label><br />");
            sb.Append($"<img src='{System.Web.HttpUtility.HtmlAttributeEncode(displayUrl)}' " +
                      $"alt='Превью' " +
                      $"style='max-width:240px; max-height:240px; object-fit:contain;' " +
                      $"class='img-thumbnail mb-2 rounded border' " +
                      $"onerror=\"this.src='{config.DefaultImagePath}';\" />");
            sb.Append("</div>");

            // 🔹 Инпут для загрузки нового
            sb.Append("<div class='mb-2'>");
            sb.Append($"<label class='form-label small'>{config.UploadLabel}:</label><br />");
            sb.Append($"<label class='btn btn-outline-primary btn-sm' for='file_{fieldName}' id='label_{fieldName}'>");
            sb.Append("<i class='bi bi-upload me-1'></i>📁 Выбрать файл");
            sb.Append("</label>");
            sb.Append($"<input type='file' name='{fieldName}' id='file_{fieldName}' " +
                      $"accept='{string.Join(",", config.AllowedExtensions)} | Макс. {config.MaxSizeBytes / (1024*1024)} МБ' " +
                      $"class='d-none' " +
                      $"onchange=\"document.getElementById('label_{fieldName}').innerHTML = this.files[0] ? '<i class=\\'bi bi-check me-1\\'></i>' + this.files[0].name : '<i class=\\'bi bi-upload me-1\\'></i>Выбрать файл';\" />");
            sb.Append("</div>");

            // 🔹 Скрытое поле со старым путём (для удаления при замене)
            sb.Append($"<input type='hidden' name='Old{fieldName}' value='{System.Web.HttpUtility.HtmlAttributeEncode(currentUrl)}' />");

            return sb.ToString();
        }
    }
}