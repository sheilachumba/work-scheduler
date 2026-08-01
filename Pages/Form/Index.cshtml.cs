using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;
using SupportWorkerPortal.Services;

namespace SupportWorkerPortal.Pages.Form;

public class IndexModel(
    ApplicationDbContext db,
    IFormValidationService validationService) : PageModel
{
    public IList<FormQuestion> Questions { get; private set; } = [];

    [BindProperty]
    public Dictionary<Guid, string> Answers { get; set; } = new();

    public Dictionary<Guid, string> FieldErrors { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Questions = await LoadActiveQuestionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Questions = await LoadActiveQuestionsAsync();
        Answers ??= new Dictionary<Guid, string>();

        FieldErrors = validationService.ValidateAnswers(Questions, Answers);
        if (FieldErrors.Count > 0)
            return Page();

        var submission = new FormSubmission
        {
            Id = Guid.NewGuid(),
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Pending
        };

        foreach (var question in Questions)
        {
            Answers.TryGetValue(question.Id, out var value);
            submission.Answers.Add(new FormAnswer
            {
                Id = Guid.NewGuid(),
                FormSubmissionId = submission.Id,
                FormQuestionId = question.Id,
                AnswerValue = value ?? string.Empty
            });
        }

        db.FormSubmissions.Add(submission);
        await db.SaveChangesAsync();

        return RedirectToPage("/Form/Success");
    }

    public string GetAnswer(Guid questionId) =>
        Answers.TryGetValue(questionId, out var value) ? value : string.Empty;

    public HashSet<string> GetMultiSelectAnswers(Guid questionId)
    {
        var raw = GetAnswer(questionId);
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw)?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task<IList<FormQuestion>> LoadActiveQuestionsAsync() =>
        await db.FormQuestions
            .Where(q => q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();
}
