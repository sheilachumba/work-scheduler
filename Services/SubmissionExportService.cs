using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;

namespace SupportWorkerPortal.Services;

public interface ISubmissionExportService
{
    Task<byte[]> ExportToCsvAsync(
        IEnumerable<Guid>? submissionIds = null,
        SubmissionStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null);
}

public class SubmissionExportService(ApplicationDbContext db) : ISubmissionExportService
{
    public async Task<byte[]> ExportToCsvAsync(
        IEnumerable<Guid>? submissionIds = null,
        SubmissionStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        string? search = null)
    {
        var query = db.FormSubmissions
            .Include(s => s.Answers)
            .ThenInclude(a => a.FormQuestion)
            .AsQueryable();

        if (submissionIds?.Any() == true)
            query = query.Where(s => submissionIds.Contains(s.Id));

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (from.HasValue)
            query = query.Where(s => s.SubmittedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(s => s.SubmittedAt <= to.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Answers.Any(a => a.AnswerValue.Contains(term)) ||
                (s.AdminNotes != null && s.AdminNotes.Contains(term)));
        }

        var submissions = await query
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();

        var questionColumns = submissions
            .SelectMany(s => s.Answers)
            .Select(a => a.FormQuestion)
            .Where(q => q is not null)
            .GroupBy(q => q!.Id)
            .Select(g => g.First()!)
            .OrderBy(q => q.DisplayOrder)
            .ToList();

        var sb = new StringBuilder();
        var headers = new List<string> { "SubmissionId", "SubmittedAt", "Status", "AdminNotes" };
        headers.AddRange(questionColumns.Select(q => q.Label));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var submission in submissions)
        {
            var row = new List<string>
            {
                submission.Id.ToString(),
                submission.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                submission.Status.ToString(),
                submission.AdminNotes ?? string.Empty
            };

            foreach (var question in questionColumns)
            {
                var answer = submission.Answers.FirstOrDefault(a => a.FormQuestionId == question.Id);
                var display = answer is null
                    ? string.Empty
                    : FormValidationService.FormatAnswerForDisplay(question, answer.AnswerValue);
                row.Add(display);
            }

            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
