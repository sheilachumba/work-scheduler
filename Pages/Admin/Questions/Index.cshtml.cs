using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SupportWorkerPortal.Data;
using SupportWorkerPortal.Models;

namespace SupportWorkerPortal.Pages.Admin.Questions;

public class IndexModel(ApplicationDbContext db) : PageModel
{
    public IList<FormQuestion> Questions { get; private set; } = [];
    public IList<FormQuestion> ActiveQuestions { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Questions = await db.FormQuestions
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        ActiveQuestions = Questions.Where(q => q.IsActive).ToList();
    }

    public async Task<IActionResult> OnPostMoveUpAsync(Guid id)
    {
        await SwapAsync(id, direction: -1);
        TempData["StatusMessage"] = "Question order updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveDownAsync(Guid id)
    {
        await SwapAsync(id, direction: 1);
        TempData["StatusMessage"] = "Question order updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id)
    {
        var question = await db.FormQuestions.FindAsync(id);
        if (question is null)
            return NotFound();

        question.IsActive = !question.IsActive;
        question.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        TempData["StatusMessage"] = question.IsActive
            ? "Question activated."
            : "Question deactivated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReorderAsync([FromBody] List<Guid> orderedIds)
    {
        if (orderedIds.Count == 0)
            return BadRequest();

        var questions = await db.FormQuestions.ToListAsync();
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var question = questions.FirstOrDefault(q => q.Id == orderedIds[i]);
            if (question is not null)
            {
                question.DisplayOrder = i + 1;
                question.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return new OkResult();
    }

    private async Task SwapAsync(Guid id, int direction)
    {
        var questions = await db.FormQuestions
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync();

        var index = questions.FindIndex(q => q.Id == id);
        if (index < 0)
            return;

        var swapIndex = index + direction;
        if (swapIndex < 0 || swapIndex >= questions.Count)
            return;

        (questions[index].DisplayOrder, questions[swapIndex].DisplayOrder) =
            (questions[swapIndex].DisplayOrder, questions[index].DisplayOrder);

        questions[index].UpdatedAt = DateTime.UtcNow;
        questions[swapIndex].UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}
