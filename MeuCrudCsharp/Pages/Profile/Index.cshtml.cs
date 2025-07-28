using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Adicione este using para o .Include()
using System.Security.Claims;

namespace MeuCrudCsharp.Pages.Profile
{
    // 1. Usando [Authorize] para garantir que apenas usuários logados acessem.
    // O sistema redirecionará para a página de login automaticamente se não estiver logado.
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApiDbContext _context;

        // Propriedades fortemente tipadas para a View. Mais seguro e prático que ViewData.
        public Users? UserProfile { get; private set; }
        public string? PaymentStatus { get; private set; }

        public IndexModel(ApiDbContext context)
        {
            _context = context;
        }

        // Tornando o método assíncrono, que é a boa prática para operações de I/O (banco de dados)
        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // A verificação de Guid ainda é uma boa prática de defesa.
            if (!Guid.TryParse(userIdString, out Guid userIdAsGuid))
            {
                // Se o usuário está autenticado mas o ID não é um Guid, algo está muito errado.
                return Unauthorized();
            }

            // 2. Buscando o usuário e seu pagamento em uma única consulta ao banco de dados com .Include()
            // Isso é mais eficiente do que fazer duas chamadas separadas.
            UserProfile = await _context.Users
                .Include(u => u.Payment_User) // Assumindo que você tem uma propriedade de navegação chamada Payment_User no seu modelo User
                .FirstOrDefaultAsync(u => u.Id == userIdAsGuid);

            if (UserProfile == null)
            {
                // Não deveria acontecer se o usuário está logado, mas é uma segurança extra.
                return NotFound("Usuário não encontrado.");
            }

            // 3. Lógica de verificação de nulo corrigida e simplificada.
            PaymentStatus = UserProfile.Payment_User?.Status;

            if (PaymentStatus == "rejected")
            {
                // Para redirecionar para outra Razor Page, o ideal é usar RedirectToPage.
                return RedirectToPage("/Payment/CreditCard"); // Ajuste o caminho conforme seu projeto.
            }

            return Page();
        }
    }
}