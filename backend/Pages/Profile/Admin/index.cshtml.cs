using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MeuCrudCsharp.Pages.Profile.Admin
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
        }
        
        public PartialViewResult OnGetSubscriptionsPartial()
        {
            return Partial("partials/_AdminSubscriptions"); 
        }
    }
}
