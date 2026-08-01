namespace SupportWorkerPortal.Models;

public class FormAnswer
{
    public Guid Id { get; set; }

    public Guid FormSubmissionId { get; set; }

    public FormSubmission FormSubmission { get; set; } = null!;

    public Guid FormQuestionId { get; set; }

    public FormQuestion FormQuestion { get; set; } = null!;

    /// <summary>Answer stored as plain text or JSON array for multi-select values.</summary>
    public string AnswerValue { get; set; } = string.Empty;
}
