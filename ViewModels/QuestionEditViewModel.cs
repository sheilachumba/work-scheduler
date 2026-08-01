using System.ComponentModel.DataAnnotations;
using SupportWorkerPortal.Models;

namespace SupportWorkerPortal.ViewModels;

public class QuestionEditViewModel
{
    public Guid? Id { get; set; }

    [Required, MaxLength(500)]
    [Display(Name = "Question label")]
    public string Label { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Field type")]
    public FieldType FieldType { get; set; } = FieldType.Text;

    [Display(Name = "Options (one per line)")]
    public string? OptionsText { get; set; }

    [Display(Name = "Required")]
    public bool IsRequired { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    [Display(Name = "Help text")]
    public string? HelpText { get; set; }
}
