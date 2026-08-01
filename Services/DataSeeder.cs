using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;

namespace SupportWorkerPortal.Services;

public interface IDataSeeder
{
    Task SeedAsync();
}

public class DataSeeder(
    ApplicationDbContext db,
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration,
    ILogger<DataSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedDefaultQuestionsAsync();
    }

    private async Task SeedRolesAsync()
    {
        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    private async Task SeedAdminUserAsync()
    {
        var email = configuration["AdminUser:Email"];
        var password = configuration["AdminUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminUser credentials not configured; skipping admin seed.");
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(user, "Admin"))
            await userManager.AddToRoleAsync(user, "Admin");
    }

    private async Task SeedDefaultQuestionsAsync()
    {
        if (await db.FormQuestions.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var order = 1;

        void Add(string label, FieldType type, bool required, string? options = null, string? help = null)
        {
            db.FormQuestions.Add(new FormQuestion
            {
                Id = Guid.NewGuid(),
                Label = label,
                FieldType = type,
                Options = options,
                IsRequired = required,
                DisplayOrder = order++,
                IsActive = true,
                HelpText = help,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        Add("What is your name?", FieldType.Text, true,
            help: "Your information is confidential and for internal use only within CCWA.");

        Add("Email address", FieldType.Email, true);

        Add("Phone number", FieldType.Phone, true);

        Add("How old are you?", FieldType.RadioButtons, false,
            options: JsonSerializer.Serialize(new[]
            {
                "< 18", "18 - 25", "26 - 35", "36 - 45", "46 - 55", "> 55", "Prefer not to say"
            }));

        Add("What Suburb do you live in?", FieldType.Text, true);

        Add("Are you studying currently? If yes, please confirm the School/College/University name or add NO STUDY",
            FieldType.Text, true);

        Add("Please confirm what you are Studying (Degree in X, Masters in X, Something else?) or add NO STUDY",
            FieldType.Text, true);

        Add("Please confirm how long (years) your Course is, and what Year you are currently in (e.g. 3 Years, Year 2) or add NO STUDY",
            FieldType.Text, true);

        Add("Please confirm the future Semester Start and End Dates for the Full Course (Add as many as you know) or add NO STUDY",
            FieldType.TextArea, true);

        Add("Does your Course have a requirement for you to complete a Placement?", FieldType.RadioButtons, true,
            options: JsonSerializer.Serialize(new[] { "Yes", "No", "NO Study" }));

        Add("Have you completed your Placement yet?", FieldType.RadioButtons, true,
            options: JsonSerializer.Serialize(new[] { "Yes", "No", "Placement not required on Course or NO Study" }));

        Add("How many Hours is your future Placement?", FieldType.RadioButtons, true,
            options: JsonSerializer.Serialize(new[]
            {
                "Less than 200", "200-500", "More than 500",
                "Placement already complete, not required or NO Study"
            }));

        Add("When is your future Placement?", FieldType.RadioButtons, true,
            options: JsonSerializer.Serialize(new[]
            {
                "I know the exact dates", "Next 3 months", "Next 3-6 months", "Next 6-12 months",
                "I dont know", "Placement already complete, not required or NO Study"
            }));

        Add("Do you have any Leave plans in the next 6 months, especially over Xmas/New Year period?",
            FieldType.RadioButtons, true,
            options: JsonSerializer.Serialize(new[] { "Yes", "No", "Nothing planned, but likely" }));

        Add("With regard to your current availability to work at CCWA, please choose the relevant choices below",
            FieldType.MultiSelectCheckbox, true,
            options: JsonSerializer.Serialize(new[]
            {
                "Mon Day (8am-6pm)", "Mon Night (6pm-8am)",
                "Tue Day (8am-6pm)", "Tue Night (6pm-8am)",
                "Wed Day (8am-6pm)", "Wed Night (6pm-8am)",
                "Thu Day (8am-6pm)", "Thu Night (6pm-8am)",
                "Fri Day (8am-6pm)", "Fri Night (6pm-8am)",
                "Sat Day (8am-6pm)", "Sat Night (6pm-8am)",
                "Sun Day (8am-6pm)", "Sun Night (6pm-8am)",
                "Very specific hours I can tell you below", "Community shifts only"
            }));

        Add("If you answered Very Specific hours in the previous Question, please add details here",
            FieldType.TextArea, false);

        Add("Preferred start date for work", FieldType.DatePicker, false);

        Add("Region you prefer to work in", FieldType.Dropdown, false,
            options: JsonSerializer.Serialize(new[]
            {
                "Perth Metro", "Peel", "South West", "Great Southern", "Mid West", "Other"
            }));

        Add("Relevant experience or additional notes", FieldType.TextArea, false);

        await db.SaveChangesAsync();
    }
}
