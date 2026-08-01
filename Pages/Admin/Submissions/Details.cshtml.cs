using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;
using SupportWorkerPortal.Services;

namespace SupportWorkerPortal.Pages.Admin.Submissions;

public class DetailsModel(ApplicationDbContext db) : PageModel
{
    public FormSubmission Submission { get; private set; } = null!;
    public IList<AnswerItem> AnswerItems { get; private set; } = [];

    [BindProperty]
    public SubmissionEditInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var submission = await LoadSubmissionAsync(id);
        if (submission is null)
            return NotFound();

        Submission = submission;
        Input = new SubmissionEditInput
        {
            Status = submission.Status,
            AdminNotes = submission.AdminNotes
        };
        AnswerItems = BuildAnswerItems(submission);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var submission = await LoadSubmissionAsync(id);
        if (submission is null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            Submission = submission;
            AnswerItems = BuildAnswerItems(submission);
            return Page();
        }

        submission.Status = Input.Status;
        submission.AdminNotes = string.IsNullOrWhiteSpace(Input.AdminNotes) ? null : Input.AdminNotes.Trim();
        await db.SaveChangesAsync();

        TempData["StatusMessage"] = "Submission updated.";
        return RedirectToPage(new { id });
    }

    private async Task<FormSubmission?> LoadSubmissionAsync(Guid id) =>
        await db.FormSubmissions
            .Include(s => s.Answers)
            .ThenInclude(a => a.FormQuestion)
            .FirstOrDefaultAsync(s => s.Id == id);

    private static IList<AnswerItem> BuildAnswerItems(FormSubmission submission) =>
        submission.Answers
            .OrderBy(a => a.FormQuestion?.DisplayOrder ?? int.MaxValue)
            .Select(a => new AnswerItem
            {
                Label = a.FormQuestion?.Label ?? "(Removed question)",
                FieldType = a.FormQuestion?.FieldType.ToString(),
                DisplayValue = a.FormQuestion is null
                    ? a.AnswerValue
                    : FormValidationService.FormatAnswerForDisplay(a.FormQuestion, a.AnswerValue)
            })
            .ToList();

    public class SubmissionEditInput
    {
        [Required]
        public SubmissionStatus Status { get; set; }

        [MaxLength(4000)]
        public string? AdminNotes { get; set; }
    }

    public class AnswerItem
    {
        public string Label { get; set; } = string.Empty;
        public string? FieldType { get; set; }
        public string DisplayValue { get; set; } = string.Empty;
    }
}
