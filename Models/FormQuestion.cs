using System.ComponentModel.DataAnnotations;

namespace SupportWorkerPortal.Models;

public class FormQuestion
{
    public Guid Id { get; set; }

    [Required, MaxLength(500)]
    public string Label { get; set; } = string.Empty;

    public FieldType FieldType { get; set; }

    /// <summary>JSON array of option strings for Dropdown, MultiSelectCheckbox, and RadioButtons.</summary>
    public string? Options { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    public string? HelpText { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<FormAnswer> Answers { get; set; } = new List<FormAnswer>();
}
