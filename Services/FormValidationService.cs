using System.Text.Json;
using System.Text.RegularExpressions;
using SupportWorkerPortal.Models;

namespace SupportWorkerPortal.Services;

public interface IFormValidationService
{
    Dictionary<Guid, string> ValidateAnswers(
        IEnumerable<FormQuestion> questions,
        IReadOnlyDictionary<Guid, string> submittedAnswers);
}

public partial class FormValidationService : IFormValidationService
{
    public Dictionary<Guid, string> ValidateAnswers(
        IEnumerable<FormQuestion> questions,
        IReadOnlyDictionary<Guid, string> submittedAnswers)
    {
        var errors = new Dictionary<Guid, string>();

        foreach (var question in questions.Where(q => q.IsActive))
        {
            submittedAnswers.TryGetValue(question.Id, out var rawValue);
            rawValue ??= string.Empty;

            if (question.IsRequired && string.IsNullOrWhiteSpace(rawValue))
            {
                errors[question.Id] = "This field is required.";
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawValue))
                continue;

            var error = question.FieldType switch
            {
                FieldType.Email when !EmailRegex().IsMatch(rawValue) => "Enter a valid email address.",
                FieldType.Phone when !PhoneRegex().IsMatch(rawValue) => "Enter a valid phone number.",
                FieldType.Number when !decimal.TryParse(rawValue, out _) => "Enter a valid number.",
                FieldType.DatePicker when !DateOnly.TryParse(rawValue, out _) => "Enter a valid date.",
                FieldType.Dropdown or FieldType.RadioButtons => ValidateSingleOption(question, rawValue),
                FieldType.MultiSelectCheckbox => ValidateMultiSelect(question, rawValue),
                _ => null
            };

            if (error is not null)
                errors[question.Id] = error;
        }

        return errors;
    }

    private static string? ValidateSingleOption(FormQuestion question, string value)
    {
        var options = ParseOptions(question.Options);
        if (options.Count == 0)
            return null;

        return options.Contains(value, StringComparer.OrdinalIgnoreCase)
            ? null
            : "Select a valid option.";
    }

    private static string? ValidateMultiSelect(FormQuestion question, string value)
    {
        var options = ParseOptions(question.Options);
        if (options.Count == 0)
            return null;

        try
        {
            var selected = JsonSerializer.Deserialize<List<string>>(value) ?? [];
            if (selected.Count == 0)
                return question.IsRequired ? "Select at least one option." : null;

            if (selected.Any(s => !options.Contains(s, StringComparer.OrdinalIgnoreCase)))
                return "One or more selected options are invalid.";

            return null;
        }
        catch (JsonException)
        {
            return "Invalid selection format.";
        }
    }

    public static List<string> ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string FormatAnswerForDisplay(FormQuestion question, string answerValue)
    {
        if (question.FieldType != FieldType.MultiSelectCheckbox)
            return answerValue;

        try
        {
            var items = JsonSerializer.Deserialize<List<string>>(answerValue) ?? [];
            return string.Join(", ", items);
        }
        catch (JsonException)
        {
            return answerValue;
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[\d\s+\-().]{7,20}$")]
    private static partial Regex PhoneRegex();
}
