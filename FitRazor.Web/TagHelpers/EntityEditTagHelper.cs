using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-edit-form")]
    public class EntityEditTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly IModelMetadataProvider _metadataProvider;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("entity-id")]
        public int EntityId { get; set; }

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Сохранить";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; }

        public EntityEditTagHelper(
            FitRazorContext context,
            IWebHostEnvironment env,
            IHttpContextAccessor httpContextAccessor,
            IHtmlGenerator htmlGenerator,
            IModelMetadataProvider metadataProvider)
        {
            _context = context;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _htmlGenerator = htmlGenerator;
            _metadataProvider = metadataProvider;
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

            var summary = _htmlGenerator.GenerateValidationSummary(
                ViewContext!,
                excludePropertyErrors: false,
                message: "",
                headerTag: null,
                htmlAttributes: new { @class = "text-danger mb-3" }
            );

            html.Insert(0, GetHtml(summary));

            var modelExplorer = _metadataProvider.GetModelExplorerForType(modelType, entity);

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
                    var inputTag = GenerateInput(modelExplorer, prop, currentValue, dropdownData, meta);
                    html.Append(GetHtml(inputTag));
                }

                // Валидация (для Razor Pages)
                var validation = _htmlGenerator.GenerateValidationMessage(
                    ViewContext!,
                    modelExplorer.GetExplorerForProperty(prop.Name),
                    prop.Name,
                    message: null,
                    tag: null,
                    htmlAttributes: new { @class = "text-danger small mt-1" }
                );
                html.Append(GetHtml(validation));

                html.Append("</div>");
            }

            html.Append("</div>");

            // 🔹 Кнопки
            html.Append("<div class='row mt-4 pt-3 border-top'>");
            html.Append("<div class='col-12 d-flex gap-2'>");
            html.Append($"<button type='submit' class='btn btn-primary px-4'><i class='bi bi-save me-2'></i>{SubmitText}</button>");

            var cancelUrl = !string.IsNullOrEmpty(CancelPage)
                ? CancelPage
                : $"/Entities/Index/{EntityName}";

            html.Append($"<a href='{cancelUrl}' class='btn btn-outline-secondary'><i class='bi bi-x-lg me-2'></i>Отмена</a>");
            html.Append("</div>");
            html.Append("</div>");

            return html.ToString();
        }

        private string GetHtml(TagBuilder tag)
        {
            using var writer = new StringWriter();
            tag.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        private TagBuilder GenerateInput(
            ModelExplorer modelExplorer,
            PropertyInfo prop,
            object? currentValue,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData,
            EntityAdminMetadata meta)
        {
            var propName = prop.Name;
            var explorer = modelExplorer.GetExplorerForProperty(propName);

            var isReadOnly = meta.ReadOnlyProperties.Contains(propName);

            var htmlAttributes = new Dictionary<string, object>
            {
                ["class"] = isReadOnly ? "form-control bg-light" : "form-control"
            };

            if (isReadOnly)
                htmlAttributes["readonly"] = "readonly";

            // 🔹 SELECT (FK)
            if (propName.EndsWith("Id") && dropdownData.ContainsKey(propName))
            {
                return _htmlGenerator.GenerateSelect(
                    ViewContext!,
                    explorer,
                    optionLabel: "— Выберите —",
                    expression: propName,
                    selectList: dropdownData[propName],
                    allowMultiple: false,
                    htmlAttributes: new { @class = "form-select" }
                );
            }

            // 🔹 STRING
            if (prop.PropertyType == typeof(string))
            {
                return _htmlGenerator.GenerateTextBox(
                    ViewContext!,
                    explorer,
                    propName,
                    currentValue,
                    format: null,
                    htmlAttributes
                );
            }

            // 🔹 INT / DECIMAL
            if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(int?) ||
                prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
            {
                htmlAttributes["type"] = "number";

                return _htmlGenerator.GenerateTextBox(
                    ViewContext!,
                    explorer,
                    propName,
                    currentValue,
                    format: null,
                    htmlAttributes
                );
            }

            // 🔹 DATETIME
            if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
            {
                htmlAttributes["type"] = "datetime-local";

                return _htmlGenerator.GenerateTextBox(
                    ViewContext!,
                    explorer,
                    propName,
                    currentValue,
                    format: "{0:yyyy-MM-ddTHH:mm}",
                    htmlAttributes
                );
            }

            if (prop.PropertyType == typeof(DateOnly) || prop.PropertyType == typeof(DateOnly?))
            {
                htmlAttributes["type"] = "date";

                return _htmlGenerator.GenerateTextBox(
                    ViewContext!,
                    explorer,
                    propName,
                    currentValue,
                    format: "{0:yyyy-MM-dd}",
                    htmlAttributes
                );
            }

            // 🔹 DEFAULT
            return _htmlGenerator.GenerateTextBox(
                ViewContext!,
                explorer,
                propName,
                currentValue,
                format: null,
                htmlAttributes
            );
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