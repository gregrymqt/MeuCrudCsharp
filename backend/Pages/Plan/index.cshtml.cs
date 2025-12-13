using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Plan
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public void OnGet() { }
    }
}
