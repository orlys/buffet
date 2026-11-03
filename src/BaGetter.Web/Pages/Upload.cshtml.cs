using BaGetter.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BaGetter.Web;

public class UploadModel : PageModel
{
    private readonly IOptionsSnapshot<UploadOptions> _options;

    public UploadModel(IOptionsSnapshot<UploadOptions> options)
    {
        _options = options;
    }

    public IActionResult OnGet()
    {
        if (!_options.Value.EnableUploadPage) return NotFound();

        return Page();
    }
}
