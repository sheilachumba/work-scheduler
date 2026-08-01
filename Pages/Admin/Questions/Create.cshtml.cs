using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;
using SupportWorkerPortal.ViewModels;

namespace SupportWorkerPortal.Pages.Admin.Questions;

public class CreateModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public QuestionEditViewModel Input { get; set; } = new();

    public IList<FormQuestion> PreviewQuestions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        PreviewQuestions = await BuildPreviewAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PreviewQuestions = await BuildPreviewAsync(includeDraft: true);

        if (!ModelState.IsValid)
            return Page();

        var maxOrder = await db.FormQuestions.MaxAsync(q => (int?)q.DisplayOrder) ?? 0;
        var now = DateTime.UtcNow;

        db.FormQuestions.Add(new FormQuestion
        {
            Id = Guid.NewGuid(),
            Label = Input.Label.Trim(),
            FieldType = Input.FieldType,
            Options = ParseOptions(Input.OptionsText, Input.FieldType),
            IsRequired = Input.IsRequired,
            IsActive = Input.IsActive,
            HelpText = string.IsNullOrWhiteSpace(Input.HelpText) ? null : Input.HelpText.Trim(),
            DisplayOrder = maxOrder + 1,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Question created.";
        return RedirectToPage("Index");
    }

    private async Task<IList<FormQuestion>> BuildPreviewAsync(bool includeDraft = false)
    {
        var questions = await db.FormQuestions
            .Where(q => q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        if (includeDraft && !string.IsNullOrWhiteSpace(Input.Label))
        {
            questions.Add(new FormQuestion
            {
                Id = Guid.Empty,
                Label = Input.Label,
                FieldType = Input.FieldType,
                Options = ParseOptions(Input.OptionsText, Input.FieldType),
                IsRequired = Input.IsRequired,
                HelpText = Input.HelpText,
                IsActive = true,
                DisplayOrder = questions.Count + 1
            });
        }

        return questions;
    }

    internal static string? ParseOptions(string? optionsText, FieldType fieldType)
    {
        if (fieldType is not (FieldType.Dropdown or FieldType.MultiSelectCheckbox or FieldType.RadioButtons))
            return null;

        var options = (optionsText ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .ToList();

        return options.Count == 0 ? "[]" : JsonSerializer.Serialize(options);
    }
}
