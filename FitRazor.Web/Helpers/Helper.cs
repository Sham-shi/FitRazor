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
    }
}
