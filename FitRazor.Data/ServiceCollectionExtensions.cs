using FitRazor.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FitRazor.Data
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDataServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Читаем выбранный провайдер (например, "MSSQL" или "SQLite")
            var dbProvider = configuration["DbProvider"];

            // 2. Получаем строку подключения по имени провайдера
            // В твоем JSON ключи в ConnectionStrings совпадают с DbProvider, это удобно
            var connectionString = configuration.GetConnectionString(dbProvider);

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    $"Строка подключения для провайдера '{dbProvider}' не найдена в конфигурации.");
            }

            // 3. Регистрируем контекст в зависимости от провайдера
            services.AddDbContext<FitRazorContext>(options =>
            {
                switch (dbProvider)
                {
                    case "MSSQL":
                        options.UseSqlServer(connectionString);
                        break;

                    case "SQLite":
                        options.UseSqlite(connectionString);
                        break;

                    default:
                        options.UseSqlite(connectionString);
                        break;
                }
            });

            return services;
        }
    }
}
