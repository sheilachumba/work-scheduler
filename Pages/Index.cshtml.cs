using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SupportWorkerPortal.Pages;

public class IndexModel : PageModel
{
    public string PublishedDate { get; private set; } = string.Empty;

    public void OnGet()
    {
        PublishedDate = DateTime.Now.ToString("d MMM yyyy");
    }
}
