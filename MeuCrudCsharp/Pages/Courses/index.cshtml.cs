using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Courses
{
    [Authorize(Policy = "ActiveSubscription")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}