using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Payment.Subscription
{
    [Authorize]
    public class indexModel : PageModel
    {
        public void OnGet() { }
    }
}
