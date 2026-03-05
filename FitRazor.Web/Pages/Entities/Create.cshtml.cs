using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FitRazor.Data;
using FitRazor.Data.Models;
using System;
using System.Threading.Tasks;

namespace FitRazor.Web.Pages.Entities
{
    [BindProperties]
    public class CreateModel : PageModel
    {
        private readonly FitRazorContext _context;

        public CreateModel(FitRazorContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string EntityName { get; set; } = "Trainers";

        // Данные формы (будут заполнены через FormCollection)
        [BindProperty]
        public IFormCollection FormData { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                switch (EntityName)
                {
                    case "Trainers":
                        await CreateTrainerAsync();
                        break;
                    case "Clients":
                        await CreateClientAsync();
                        break;
                    case "Services":
                        await CreateServiceAsync();
                        break;
                    case "Bookings":
                        await CreateBookingAsync();
                        break;
                    case "TrainerServices":
                        await CreateTrainerServiceAsync();
                        break;
                    default:
                        throw new ArgumentException($"Неизвестная сущность: {EntityName}");
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Запись успешно создана!";
                return RedirectToPage("Index", new { entityName = EntityName });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
                return RedirectToPage("Create", new { entityName = EntityName });
            }
        }

        private async Task CreateTrainerAsync()
        {
            var trainer = new Trainer
            {
                FullName = Request.Form["FullName"],
                Phone = Request.Form["Phone"],
                Email = Request.Form["Email"],
                Slogan = Request.Form["Slogan"],
                Specialization = Request.Form["Specialization"],
                SpecializationDescription = Request.Form["SpecializationDescription"],
                Motto = Request.Form["Motto"],
                Education = Request.Form["Education"],
                WorkExperience = Request.Form["WorkExperience"],
                SportsAchievements = Request.Form["SportsAchievements"],
                Salary = decimal.Parse(Request.Form["Salary"]),
                PhotoUrl = Request.Form["PhotoUrl"]
            };

            _context.Trainers.Add(trainer);
        }

        private async Task CreateClientAsync()
        {
            var client = new Client
            {
                FullName = Request.Form["FullName"],
                Phone = Request.Form["Phone"],
                Email = Request.Form["Email"],
                BirthDate = string.IsNullOrEmpty(Request.Form["BirthDate"])
                    ? null
                    : DateOnly.Parse(Request.Form["BirthDate"]),
                RegistrationDate = DateOnly.FromDateTime(DateTime.Today)
            };

            _context.Clients.Add(client);
        }

        private async Task CreateServiceAsync()
        {
            var service = new Service
            {
                ServiceName = Request.Form["ServiceName"],
                DurationMinutes = int.Parse(Request.Form["DurationMinutes"]),
                BasePrice = decimal.Parse(Request.Form["BasePrice"]),
                Description = Request.Form["Description"]
            };

            _context.Services.Add(service);
        }

        private async Task CreateBookingAsync()
        {
            var booking = new Booking
            {
                ClientId = int.Parse(Request.Form["ClientId"]),
                TrainerServiceId = int.Parse(Request.Form["TrainerServiceId"]),
                BookingDateTime = DateTime.Parse(Request.Form["BookingDateTime"]),
                SessionsCount = int.Parse(Request.Form["SessionsCount"]),
                UnitPrice = decimal.Parse(Request.Form["UnitPrice"]),
                TotalPrice = decimal.Parse(Request.Form["UnitPrice"]) * int.Parse(Request.Form["SessionsCount"]),
                Status = Request.Form["Status"],
                Notes = Request.Form["Notes"],
                CreatedDate = DateTime.Now
            };

            _context.Bookings.Add(booking);
        }

        private async Task CreateTrainerServiceAsync()
        {
            var trainerService = new TrainerService
            {
                TrainerId = int.Parse(Request.Form["TrainerId"]),
                ServiceId = int.Parse(Request.Form["ServiceId"])
            };

            _context.TrainerServices.Add(trainerService);
        }
    }
}