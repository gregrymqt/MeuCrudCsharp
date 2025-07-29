using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Pages.Videos
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApiDbContext _context;

        // --- PROPRIEDADES ATUALIZADAS ---
        public Users? UserProfile { get; private set; }
        public string? PaymentStatus { get; private set; }

        // Propriedade para receber o ID do vídeo da URL
        [BindProperty(SupportsGet = true)]
        public string? VideoId { get; set; }

        // Propriedade para enviar os dados do vídeo para a página
        public Video? VideoData { get; private set; } // Supondo que você tenha um modelo 'Video'

        public IndexModel(ApiDbContext context)
        {
            _context = context;
        }

        // O nome do método não precisa mudar, o [BindProperty] faz a mágica
        public async Task<IActionResult> OnGetAsync()
        {
            // ====================================================================
            // ETAPA 1: A SUA LÓGICA DE AUTORIZAÇÃO (QUE JÁ ESTÁ PERFEITA)
            // ====================================================================
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out Guid userIdAsGuid))
            {
                return Unauthorized();
            }

            UserProfile = await _context
                .Users.Include(u => u.Payment_User)
                .FirstOrDefaultAsync(u => u.Id == userIdAsGuid);

            if (UserProfile == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            PaymentStatus = UserProfile.Payment_User?.Status;
            if (PaymentStatus == "rejected")
            {
                return RedirectToPage("/Payment/CreditCard");
            }

            // ====================================================================
            // ETAPA 2: A NOVA LÓGICA PARA BUSCAR O VÍDEO
            // (Só executa se o usuário tiver permissão)
            // ====================================================================

            if (string.IsNullOrEmpty(VideoId))
            {
                return BadRequest("O ID do vídeo não foi fornecido.");
            }

            // Supondo que seu VideoId no banco seja um Guid também
            if (!Guid.TryParse(VideoId, out Guid videoIdAsGuid))
            {
                return BadRequest("O ID do vídeo é inválido.");
            }

            // Busca os dados do vídeo específico no banco de dados
            // Supondo que você tenha uma tabela "Videos" no seu ApiDbContext
            VideoData = await _context.Videos.FirstOrDefaultAsync(v => v.Id == videoIdAsGuid);

            if (VideoData == null)
            {
                return NotFound("O vídeo solicitado não foi encontrado.");
            }

            return Page();
        }
    }
}
