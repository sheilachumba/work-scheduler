using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;
using SupportWorkerPortal.Services;
using SupportWorkerPortal.ViewModels;

namespace SupportWorkerPortal.Pages.Admin.Questions;

public class EditModel(ApplicationDbContext db) : PageModel
{
    [BindProperty]
    public QuestionEditViewModel Input { get; set; } = new();

    public IList<FormQuestion> PreviewQuestions { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var question = await db.FormQuestions.FindAsync(id);
        if (question is null)
            return NotFound();

        Input = MapToViewModel(question);
        PreviewQuestions = await BuildPreviewAsync(question);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var question = await db.FormQuestions.FindAsync(id);
        if (question is null)
            return NotFound();

        PreviewQuestions = await BuildPreviewAsync(question, useDraft: true);

        if (!ModelState.IsValid)
            return Page();

        question.Label = Input.Label.Trim();
        question.FieldType = Input.FieldType;
        question.Options = CreateModel.ParseOptions(Input.OptionsText, Input.FieldType);
        question.IsRequired = Input.IsRequired;
        question.IsActive = Input.IsActive;
        question.HelpText = string.IsNullOrWhiteSpace(Input.HelpText) ? null : Input.HelpText.Trim();
        question.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        TempData["StatusMessage"] = "Question updated.";
        return RedirectToPage("Index");
    }

    private static QuestionEditViewModel MapToViewModel(FormQuestion question)
    {
        var options = FormValidationService.ParseOptions(question.Options);
        return new QuestionEditViewModel
        {
            Id = question.Id,
            Label = question.Label,
            FieldType = question.FieldType,
            OptionsText = options.Count == 0 ? string.Empty : string.Join('\n', options),
            IsRequired = question.IsRequired,
            IsActive = question.IsActive,
            HelpText = question.HelpText
        };
    }

    private async Task<IList<FormQuestion>> BuildPreviewAsync(FormQuestion current, bool useDraft = false)
    {
        var questions = await db.FormQuestions
            .Where(q => q.IsActive && q.Id != current.Id)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        var preview = useDraft && !string.IsNullOrWhiteSpace(Input.Label)
            ? new FormQuestion
            {
                Id = current.Id,
                Label = Input.Label,
                FieldType = Input.FieldType,
                Options = CreateModel.ParseOptions(Input.OptionsText, Input.FieldType),
                IsRequired = Input.IsRequired,
                HelpText = Input.HelpText,
                IsActive = Input.IsActive,
                DisplayOrder = current.DisplayOrder
            }
            : current;

        if (preview.IsActive)
            questions.Add(preview);

        return questions.OrderBy(q => q.DisplayOrder).ToList();
    }
}
