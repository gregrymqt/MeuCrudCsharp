using System.Security.Claims;
using MeuCrudCsharp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Pages.Course
{
    public class IndexModel : PageModel
    {
        private readonly ApiDbContext _context;

        public IndexModel(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // 2. Verifica se o usuário é um Admin. Se for, ele tem acesso livre.
            if (User.IsInRole("Admin"))
            {
                return Page(); // Admins podem ver a página sem checagem de pagamento.
            }

            // 3. Se não for admin, verifica o status do pagamento.
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString))
            {
                return Forbid(); // Se não encontrar o ID do usuário, nega o acesso.
            }

            // Busca o usuário e seu status de pagamento em uma única consulta.
            // Busca o usuário e o status do pagamento aprovado em uma única consulta.
            var userPaymentStatus = await _context
                .Users.Where(u => u.Id == userIdString)
                .SelectMany(u => u.Payments)
                .Where(p => p.Status == "approved")
                .Select(p => p.Status)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(userPaymentStatus))
            {
                return Page(); // Se o pagamento está aprovado, permite o acesso.
            }

            return RedirectToPage(
                "/Payment/CreditCard",
                new { mensage = "Voce precisa pagar para acessar essa área" }
            );
        }
    }
}
