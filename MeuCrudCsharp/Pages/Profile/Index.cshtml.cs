using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // Adicione este using para o .Include()

namespace MeuCrudCsharp.Pages.Profile
{
    // 1. Usando [Authorize] para garantir que apenas usu�rios logados acessem.
    // O sistema redirecionar� para a p�gina de login automaticamente se n�o estiver logado.
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApiDbContext _context;

        // Propriedades fortemente tipadas para a View. Mais seguro e pr�tico que ViewData.
        public Users? UserProfile { get; private set; }
        public string? PaymentStatus { get; private set; }

        public IndexModel(ApiDbContext context)
        {
            _context = context;
        }

        // Tornando o m�todo ass�ncrono, que � a boa pr�tica para opera��es de I/O (banco de dados)
        public async Task<IActionResult> OnGetAsync()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                return NotFound("Usu��rio n��o encontrado.");
            }

            // 2. Buscando o usu�rio e seu pagamento em uma �nica consulta ao banco de dados com .Include()
            // Isso � mais eficiente do que fazer duas chamadas separadas.
            UserProfile = await _context
                .Users.Include(u => u.Payment_User) // Assumindo que voc� tem uma propriedade de navega��o chamada Payment_User no seu modelo User
                .FirstOrDefaultAsync(u => u.Id == userIdString);

            if (UserProfile == null)
            {
                // N�o deveria acontecer se o usu�rio est� logado, mas � uma seguran�a extra.
                return NotFound("Usu�rio n�o encontrado.");
            }

            // 3. L�gica de verifica��o de nulo corrigida e simplificada.
            PaymentStatus = UserProfile.Payment_User?.Status;

            if (PaymentStatus == "rejected")
            {
                // Para redirecionar para outra Razor Page, o ideal � usar RedirectToPage.
                return RedirectToPage("/Payment/CreditCard"); // Ajuste o caminho conforme seu projeto.
            }

            return Page();
        }
    }
}
