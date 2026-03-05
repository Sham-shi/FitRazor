using FitRazor.Web.TagHelpers;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FitRazor.Web.Helpers
{
    public static class Helper
    {
        /// <summary>
        /// Парсит строковое значение из формы в нужный тип свойства
        /// </summary>
        public static object? ParseValue(string? rawValue, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(rawValue)) return null;

            try
            {
                if (targetType == typeof(string)) return rawValue;
                if (targetType == typeof(int)) return int.Parse(rawValue);
                if (targetType == typeof(int?)) return int.TryParse(rawValue, out var i) ? i : (int?)null;
                if (targetType == typeof(decimal)) return decimal.Parse(rawValue.Replace(',', '.'));
                if (targetType == typeof(decimal?)) return decimal.TryParse(rawValue.Replace(',', '.'), out var d) ? d : (decimal?)null;
                if (targetType == typeof(DateTime)) return DateTime.Parse(rawValue);
                if (targetType == typeof(DateTime?)) return DateTime.TryParse(rawValue, out var dt) ? dt : (DateTime?)null;
                if (targetType == typeof(DateOnly)) return DateOnly.Parse(rawValue);
                if (targetType == typeof(DateOnly?)) return DateOnly.TryParse(rawValue, out var d) ? d : (DateOnly?)null;

                // Можно добавить bool, enum и т.д. при необходимости
                return Convert.ChangeType(rawValue, targetType);
            }
            catch
            {
                return null; // или throw, если хочешь строгую валидацию
            }
        }

        /// <summary>
        /// Применяет значения из формы к объекту сущности через рефлексию
        /// </summary>
        public static void ApplyFormValuesToEntity(object entity, IFormCollection form)
        {
            var type = entity.GetType();
            foreach (var entry in form)
            {
                var propName = entry.Key;
                var rawValue = entry.Value.ToString();

                var prop = type.GetProperty(propName);
                if (prop == null || !prop.CanWrite) continue;

                var parsed = ParseValue(rawValue, prop.PropertyType);
                if (parsed != null || prop.PropertyType.IsValueType == false) // null допустим для reference / nullable
                {
                    prop.SetValue(entity, parsed);
                }
            }
        }

        /// <summary>
        /// Возвращает свойства для отображения на UI
        /// </summary>
        public static IEnumerable<PropertyInfo> GetFormProperties(Type type)
        {
            return type.GetProperties()
                .Where(p =>
                    p.CanWrite &&
                    p.CanRead &&
                    !p.PropertyType.IsGenericType &&
                    // Исключаем только PK, но оставляем FK (ClientId, TrainerId...)
                    p.Name != "Id" &&
                    p.Name != $"{type.Name}Id" &&
                    // 👇 Добавляем проверку на ScaffoldColumn
                    p.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold != false)
                .OrderBy(p =>
                {
                    var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
                    return displayAttr?.GetOrder() ?? 1000;
                });
        }

        /// <summary>
        /// Сохраняет загруженный файл изображения в папку wwwroot/Images/{subfolder},
        /// возвращает относительный путь или null, если файл не был передан/невалиден.
        /// При успешном сохранении может удалить старый файл, если передан oldPath.
        /// </summary>
        public static async Task<string?> SaveImageAsync(
            IFormFile? file,
            IWebHostEnvironment env,
            string subfolder,
            string? oldPath = null,
            long maxSizeBytes = 5 * 1024 * 1024,
            string[]? allowedExtensions = null)
        {
            if (file == null || file.Length == 0)
                return null;

            allowedExtensions ??= new[] { ".jpg", ".jpeg", ".png" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Разрешены только файлы: {string.Join(", ", allowedExtensions)}");
            }

            if (file.Length > maxSizeBytes)
            {
                throw new ArgumentException($"Файл слишком большой (максимум {maxSizeBytes / (1024 * 1024)} МБ)");
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var imagesFolder = Path.Combine(env.WebRootPath, "Images", subfolder);
            Directory.CreateDirectory(imagesFolder);

            var fullPath = Path.Combine(imagesFolder, fileName);
            var relativePath = $"/Images/{subfolder}/{fileName}";

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Удаляем старый файл, если он был и не является заглушкой
            if (!string.IsNullOrEmpty(oldPath) &&
                !oldPath.Contains("no-photo.jpg", StringComparison.OrdinalIgnoreCase) &&
                oldPath.StartsWith("/"))
            {
                var oldPhysicalPath = Path.Combine(env.WebRootPath, oldPath.TrimStart('/'));
                if (File.Exists(oldPhysicalPath))
                {
                    try
                    {
                        File.Delete(oldPhysicalPath);
                    }
                    catch
                    {
                        // игнорируем ошибки удаления (файл может быть заблокирован, права и т.д.)
                    }
                }
            }

            return relativePath;
        }
    }
}
