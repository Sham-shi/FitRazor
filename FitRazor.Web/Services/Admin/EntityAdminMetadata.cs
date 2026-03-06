using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitRazor.Data;
using FitRazor.Data.Models;

namespace FitRazor.Web.Services.Admin
{
    public class EntityAdminMetadata
    {
        public string Name { get; init; } = null!;
        public string PluralDisplayName { get; init; } = null!;
        public Type EntityType { get; init; } = null!;
        public string KeyPropertyName { get; init; } = "Id";

        // Как загрузить список записей
        public Func<FitRazorContext, IQueryable<object>> QueryFactory { get; init; } = null!;

        // Получить одну запись по ID (с Include где нужно)
        public Func<FitRazorContext, int, Task<object?>> GetByIdAsync { get; init; } = null!;

        // Существует ли запись с таким ID
        public Func<FitRazorContext, int, Task<bool>> ExistsAsync { get; init; } = null!;

        // Удаление + бизнес-проверки
        public Func<FitRazorContext, int, Task<(bool Success, string? Error)>> DeleteAsync { get; init; } = null!;

        // Какое свойство/выражение использовать для красивого имени в подтверждении удаления
        public Func<FitRazorContext, int, Task<string>> GetDisplayNameForDeleteAsync { get; init; } = null!;

        // Провайдеры данных для выпадающих списков (FK)
        public Dictionary<string, Func<FitRazorContext, Task<IEnumerable<SelectListItem>>>> DropdownProviders { get; init; } = new();
    }

    public static class EntityAdminRegistry
    {
        private static readonly Dictionary<string, EntityAdminMetadata> _registry;

        static EntityAdminRegistry()
        {
            _registry = new Dictionary<string, EntityAdminMetadata>(StringComparer.OrdinalIgnoreCase);

            // ────────────────────────────────────────────────
            // Trainers
            // ────────────────────────────────────────────────
            _registry["Trainers"] = new EntityAdminMetadata
            {
                Name = "Trainers",
                PluralDisplayName = "Тренеры",
                EntityType = typeof(Trainer),
                KeyPropertyName = "TrainerId",

                QueryFactory = ctx => ctx.Trainers.AsQueryable<object>(),

                GetByIdAsync = async (ctx, id) => await ctx.Trainers.FindAsync(id),

                ExistsAsync = (ctx, id) => ctx.Trainers.AnyAsync(t => t.TrainerId == id),

                DeleteAsync = async (ctx, id) =>
                {
                    var trainer = await ctx.Trainers.FindAsync(id);
                    if (trainer == null) return (false, "Тренер не найден");

                    if (await ctx.TrainerServices.AnyAsync(ts => ts.TrainerId == id))
                        return (false, "Нельзя удалить тренера — есть назначенные услуги");

                    ctx.Trainers.Remove(trainer);
                    await ctx.SaveChangesAsync();
                    return (true, null);
                },

                GetDisplayNameForDeleteAsync = async (ctx, id) =>
                {
                    var t = await ctx.Trainers.FindAsync(id);
                    return t?.FullName ?? $"Тренер #{id}";
                },

                DropdownProviders = { }
            };

            // ────────────────────────────────────────────────
            // Clients
            // ────────────────────────────────────────────────
            _registry["Clients"] = new EntityAdminMetadata
            {
                Name = "Clients",
                PluralDisplayName = "Клиенты",
                EntityType = typeof(Client),
                KeyPropertyName = "ClientId",

                QueryFactory = ctx => ctx.Clients.AsQueryable<object>(),

                GetByIdAsync = async (ctx, id) => await ctx.Clients.FindAsync(id),

                ExistsAsync = (ctx, id) => ctx.Clients.AnyAsync(c => c.ClientId == id),

                DeleteAsync = async (ctx, id) =>
                {
                    var client = await ctx.Clients.FindAsync(id);
                    if (client == null) return (false, "Клиент не найден");

                    if (await ctx.Bookings.AnyAsync(b => b.ClientId == id))
                        return (false, "Нельзя удалить клиента — есть активные бронирования");

                    ctx.Clients.Remove(client);
                    await ctx.SaveChangesAsync();
                    return (true, null);
                },

                GetDisplayNameForDeleteAsync = async (ctx, id) =>
                {
                    var c = await ctx.Clients.FindAsync(id);
                    return c?.FullName ?? $"Клиент #{id}";
                },

                DropdownProviders = { }
            };

            // ────────────────────────────────────────────────
            // Services
            // ────────────────────────────────────────────────
            _registry["Services"] = new EntityAdminMetadata
            {
                Name = "Services",
                PluralDisplayName = "Услуги",
                EntityType = typeof(Service),
                KeyPropertyName = "ServiceId",

                QueryFactory = ctx => ctx.Services.AsQueryable<object>(),

                GetByIdAsync = async (ctx, id) => await ctx.Services.FindAsync(id),

                ExistsAsync = (ctx, id) => ctx.Services.AnyAsync(s => s.ServiceId == id),

                DeleteAsync = async (ctx, id) =>
                {
                    var svc = await ctx.Services.FindAsync(id);
                    if (svc == null) return (false, "Услуга не найдена");

                    if (await ctx.TrainerServices.AnyAsync(ts => ts.ServiceId == id))
                        return (false, "Нельзя удалить услугу — она назначена тренерам");

                    ctx.Services.Remove(svc);
                    await ctx.SaveChangesAsync();
                    return (true, null);
                },

                GetDisplayNameForDeleteAsync = async (ctx, id) =>
                {
                    var s = await ctx.Services.FindAsync(id);
                    return s?.ServiceName ?? $"Услуга #{id}";
                },

                DropdownProviders = { }
            };

            // ────────────────────────────────────────────────
            // Bookings
            // ────────────────────────────────────────────────
            _registry["Bookings"] = new EntityAdminMetadata
            {
                Name = "Bookings",
                PluralDisplayName = "Записи",
                EntityType = typeof(Booking),
                KeyPropertyName = "BookingId",

                QueryFactory = ctx => ctx.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.TrainerService)
                    .ThenInclude(ts => ts!.Trainer)
                    .AsQueryable<object>(),

                GetByIdAsync = async (ctx, id) =>
                    await ctx.Bookings
                        .Include(b => b.Client)
                        .Include(b => b.TrainerService)
                        .FirstOrDefaultAsync(b => b.BookingId == id),

                ExistsAsync = (ctx, id) => ctx.Bookings.AnyAsync(b => b.BookingId == id),

                DeleteAsync = async (ctx, id) =>
                {
                    var booking = await ctx.Bookings.FindAsync(id);
                    if (booking == null) return (false, "Запись не найдена");

                    ctx.Bookings.Remove(booking);
                    await ctx.SaveChangesAsync();
                    return (true, null);
                },

                GetDisplayNameForDeleteAsync = async (ctx, id) =>
                {
                    var b = await ctx.Bookings
                        .Include(b => b.Client)
                        .Include(b => b.TrainerService)
                        .ThenInclude(ts => ts!.Service)
                        .FirstOrDefaultAsync(b => b.BookingId == id);

                    if (b == null) return $"Запись #{id}";

                    var client = b.Client?.FullName ?? "Клиент";
                    var service = b.TrainerService?.Service?.ServiceName ?? "Услуга";
                    return $"{client} — {service} ({b.BookingDateTime:dd.MM.yyyy HH:mm})";
                },
                DropdownProviders =
                {
                    ["ClientId"] = async ctx => await ctx.Clients
                        .Select(c => new SelectListItem(c.FullName, c.ClientId.ToString())) // ✅ Текст, затем Значение
                        .ToListAsync(),

                    ["TrainerServiceId"] = async ctx => await ctx.TrainerServices
                        .Include(ts => ts.Trainer)
                        .Include(ts => ts.Service)
                        .Select(ts => new SelectListItem(
                            $"{ts.Trainer!.FullName} — {ts.Service!.ServiceName}", // Текст (что видим)
                            ts.TrainerServiceId.ToString()))                       // Значение (что отправляем)
                        .ToListAsync()
                }
            };

            // ────────────────────────────────────────────────
            // TrainerServices
            // ────────────────────────────────────────────────
            _registry["TrainerServices"] = new EntityAdminMetadata
            {
                Name = "TrainerServices",
                PluralDisplayName = "Услуги тренеров",
                EntityType = typeof(TrainerService),
                KeyPropertyName = "TrainerServiceId",

                QueryFactory = ctx => ctx.TrainerServices
                    .Include(ts => ts.Trainer)
                    .Include(ts => ts.Service)
                    .AsQueryable<object>(),

                GetByIdAsync = async (ctx, id) =>
                    await ctx.TrainerServices
                        .Include(ts => ts.Trainer)
                        .Include(ts => ts.Service)
                        .FirstOrDefaultAsync(ts => ts.TrainerServiceId == id),

                ExistsAsync = (ctx, id) => ctx.TrainerServices.AnyAsync(ts => ts.TrainerServiceId == id),

                DeleteAsync = async (ctx, id) =>
                {
                    var ts = await ctx.TrainerServices.FindAsync(id);
                    if (ts == null) return (false, "Связь не найдена");

                    if (await ctx.Bookings.AnyAsync(b => b.TrainerServiceId == id))
                        return (false, "Нельзя удалить связь — есть активные бронирования");

                    ctx.TrainerServices.Remove(ts);
                    await ctx.SaveChangesAsync();
                    return (true, null);
                },

                GetDisplayNameForDeleteAsync = async (ctx, id) =>
                {
                    var ts = await ctx.TrainerServices
                        .Include(ts => ts.Trainer)
                        .Include(ts => ts.Service)
                        .FirstOrDefaultAsync(ts => ts.TrainerServiceId == id);

                    if (ts == null) return $"Связь #{id}";

                    return $"{ts.Trainer?.FullName} — {ts.Service?.ServiceName}";
                },

                DropdownProviders =
                {
                    ["TrainerId"] = async ctx => await ctx.Trainers
                        .Select(t => new SelectListItem(
                            t.FullName,              // ✅ 1. Текст (что видит пользователь)
                            t.TrainerId.ToString())) // ✅ 2. Значение (что сохраняется в БД)
                        .ToListAsync(),

                    ["ServiceId"] = async ctx => await ctx.Services
                        .Select(s => new SelectListItem(
                            s.ServiceName,           // ✅ 1. Текст
                            s.ServiceId.ToString())) // ✅ 2. Значение
                        .ToListAsync()
                }
            };
        }

        public static EntityAdminMetadata? Get(string? entityName)
        {
            if (string.IsNullOrWhiteSpace(entityName)) return null;
            _registry.TryGetValue(entityName, out var meta);
            return meta;
        }

        public static IEnumerable<EntityAdminMetadata> All => _registry.Values;
    }
}
