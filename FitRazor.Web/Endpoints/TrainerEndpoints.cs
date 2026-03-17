using FitRazor.Data.Models;
using FitRazor.Endpoints.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace FitRazor.Endpoints;

public static class TrainerEndpoints
{
    public static void MapTrainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trainers")
                       .WithTags("Trainers")
                       .RequireAuthorization("AdminOnly");

        // GET: /api/trainers
        group.MapGet("/", async (FitRazorContext db) =>
        {
            var trainers = await db.Trainers.ToListAsync();
            return Results.Ok(trainers.Select(t => new TrainerResponseDto
            {
                TrainerId = t.TrainerId,
                FullName = t.FullName,
                Phone = t.Phone,
                Email = t.Email,
                Slogan = t.Slogan,
                Specialization = t.Specialization,
                SpecializationDescription = t.SpecializationDescription,
                Motto = t.Motto,
                Education = t.Education,
                WorkExperience = t.WorkExperience,
                SportsAchievements = t.SportsAchievements,
                PhotoUrl = t.PhotoUrl
            }));
        })
        .WithName("GetAllTrainers");

        // GET: /api/trainers/{id}
        group.MapGet("/{id:int}", async (int id, FitRazorContext db) =>
        {
            var trainer = await db.Trainers.FindAsync(id);
            if (trainer is null) return Results.NotFound($"Тренер {id} не найден.");

            return Results.Ok(new TrainerResponseDto
            {
                TrainerId = trainer.TrainerId,
                FullName = trainer.FullName,
                Phone = trainer.Phone,
                Email = trainer.Email,
                Slogan = trainer.Slogan,
                Specialization = trainer.Specialization,
                SpecializationDescription = trainer.SpecializationDescription,
                Motto = trainer.Motto,
                Education = trainer.Education,
                WorkExperience = trainer.WorkExperience,
                SportsAchievements = trainer.SportsAchievements,
                PhotoUrl = trainer.PhotoUrl
            });
        })
        .WithName("GetTrainerById");

        // POST: /api/trainers
        group.MapPost("/", async (CreateTrainerDto dto, FitRazorContext db) =>
        {
            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(dto, context, results, true))
            {
                var errors = results.GroupBy(r => r.MemberNames.First())
                                   .ToDictionary(g => g.Key, g => g.Select(r => r.ErrorMessage).ToArray());
                return Results.BadRequest(new ValidationProblemDetails(errors));
            }

            var trainer = new Trainer
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Slogan = dto.Slogan,
                Specialization = dto.Specialization,
                SpecializationDescription = dto.SpecializationDescription,
                Motto = dto.Motto,
                Education = dto.Education,
                WorkExperience = dto.WorkExperience,
                SportsAchievements = dto.SportsAchievements,
                PhotoUrl = dto.PhotoUrl,
                Salary = 0
            };

            db.Trainers.Add(trainer);
            await db.SaveChangesAsync();

            var response = new TrainerResponseDto
            {
                TrainerId = trainer.TrainerId,
                FullName = trainer.FullName,
                Phone = trainer.Phone,
                Email = trainer.Email,
                Specialization = trainer.Specialization
            };

            return Results.Created($"/api/trainers/{trainer.TrainerId}", response);
        })
        .WithName("CreateTrainer")
        .Produces<TrainerResponseDto>(201)
        .Produces<ValidationProblemDetails>(400);

        // PUT: /api/trainers/{id}
        group.MapPut("/{id:int}", async (int id, UpdateTrainerDto dto, FitRazorContext db) =>
        {
            var trainer = await db.Trainers.FindAsync(id);
            if (trainer is null) return Results.NotFound();

            if (!string.IsNullOrEmpty(dto.FullName)) trainer.FullName = dto.FullName;
            if (!string.IsNullOrEmpty(dto.Phone)) trainer.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Email)) trainer.Email = dto.Email;
            if (dto.Slogan != null) trainer.Slogan = dto.Slogan;
            if (!string.IsNullOrEmpty(dto.Specialization)) trainer.Specialization = dto.Specialization;
            if (dto.SpecializationDescription != null) trainer.SpecializationDescription = dto.SpecializationDescription;
            if (dto.Motto != null) trainer.Motto = dto.Motto;
            if (!string.IsNullOrEmpty(dto.Education)) trainer.Education = dto.Education;
            if (!string.IsNullOrEmpty(dto.WorkExperience)) trainer.WorkExperience = dto.WorkExperience;
            if (!string.IsNullOrEmpty(dto.SportsAchievements)) trainer.SportsAchievements = dto.SportsAchievements;
            if (dto.PhotoUrl != null) trainer.PhotoUrl = dto.PhotoUrl;

            await db.SaveChangesAsync();
            return Results.Ok();
        })
        .WithName("UpdateTrainer");

        // DELETE: /api/trainers/{id}
        group.MapDelete("/{id:int}", async (int id, FitRazorContext db) =>
        {
            var trainer = await db.Trainers.FindAsync(id);
            if (trainer is null) return Results.NotFound();

            db.Trainers.Remove(trainer);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteTrainer");
    }
}