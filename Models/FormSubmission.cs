using System.ComponentModel.DataAnnotations;

namespace SupportWorkerPortal.Models;

public class FormSubmission
{
    public Guid Id { get; set; }

    public DateTime SubmittedAt { get; set; }

    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;

    [MaxLength(4000)]
    public string? AdminNotes { get; set; }

    public ICollection<FormAnswer> Answers { get; set; } = new List<FormAnswer>();
}
