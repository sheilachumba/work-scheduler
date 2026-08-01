using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;
using SupportWorkerPortal.Services;

namespace SupportWorkerPortal.Pages.Admin.Submissions;

public class IndexModel(ApplicationDbContext db, ISubmissionExportService exportService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public SubmissionFilter Filter { get; set; } = new();

    public IList<SubmissionRow> Submissions { get; private set; } = [];
    public int TotalCount { get; private set; }
    public int PendingCount { get; private set; }
    public int ApprovedCount { get; private set; }
    public IList<SelectListItem> StatusOptions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        StatusOptions = Enum.GetValues<SubmissionStatus>()
            .Select(s => new SelectListItem(s.ToString(), s.ToString()))
            .ToList();

        TotalCount = await db.FormSubmissions.CountAsync();
        PendingCount = await db.FormSubmissions.CountAsync(s => s.Status == SubmissionStatus.Pending);
        ApprovedCount = await db.FormSubmissions.CountAsync(s => s.Status == SubmissionStatus.Approved);

        var query = db.FormSubmissions
            .Include(s => s.Answers)
            .AsQueryable();

        if (Filter.Status.HasValue)
            query = query.Where(s => s.Status == Filter.Status.Value);

        if (Filter.From.HasValue)
            query = query.Where(s => s.SubmittedAt >= Filter.From.Value);

        if (Filter.To.HasValue)
            query = query.Where(s => s.SubmittedAt <= Filter.To.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(Filter.Search))
        {
            var term = Filter.Search.Trim();
            query = query.Where(s =>
                s.Answers.Any(a => a.AnswerValue.Contains(term)) ||
                (s.AdminNotes != null && s.AdminNotes.Contains(term)));
        }

        var results = await query
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        Submissions = results.Select(s => new SubmissionRow
        {
            Id = s.Id,
            SubmittedAt = s.SubmittedAt,
            Status = s.Status,
            PreviewText = s.Answers.FirstOrDefault()?.AnswerValue
        }).ToList();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var bytes = await exportService.ExportToCsvAsync(
            status: Filter.Status,
            from: Filter.From,
            to: Filter.To,
            search: Filter.Search);

        return File(bytes, "text/csv", $"submissions-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }

    public string StatusBadgeClass(SubmissionStatus status) => status switch
    {
        SubmissionStatus.Approved => "text-bg-success",
        SubmissionStatus.Rejected => "text-bg-danger",
        SubmissionStatus.Waitlisted => "text-bg-info",
        _ => "text-bg-warning"
    };

    public class SubmissionFilter
    {
        public SubmissionStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
    }

    public class SubmissionRow
    {
        public Guid Id { get; set; }
        public DateTime SubmittedAt { get; set; }
        public SubmissionStatus Status { get; set; }
        public string? PreviewText { get; set; }
    }
}
