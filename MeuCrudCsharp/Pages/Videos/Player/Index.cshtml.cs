using System.Security.Claims;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Pages.Videos.Player
{
    [Authorize] // Garante que apenas usuários logados acessem esta página
    public class IndexModel : PageModel
    {
        private readonly ApiDbContext _context;

        // Propriedade para receber o ID do vídeo da URL (graças ao @page "{videoId}")
        [BindProperty(SupportsGet = true)]
        public string? VideoId { get; set; }

        // Propriedades para enviar dados para o HTML
        public Video? VideoData { get; private set; }
        public Users? UserProfile { get; private set; }

        public IndexModel(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // --- ETAPA 1: Validar o Usuário (Lógica que você já tinha) ---
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            UserProfile = await _context
                .Users.Include(u => u.Payments)
                .FirstOrDefaultAsync(u => u.Id == userIdString);

            if (UserProfile == null || UserProfile.Payments.Any(p => p.Status == "rejected"))
            {
                // Se não encontrar o usuário ou o pagamento foi rejeitado, nega o acesso.
                // Você pode redirecionar para a página de pagamento aqui.
                return Forbid();
            }

            // --- ETAPA 2: Validar e Buscar o Vídeo ---
            if (string.IsNullOrEmpty(VideoId))
            {
                return NotFound("O ID do vídeo não foi fornecido na URL.");
            }

            // Busca o vídeo no banco usando o StorageIdentifier, que é o GUID da pasta
            VideoData = await _context.Videos.FirstOrDefaultAsync(v =>
                v.StorageIdentifier == VideoId
            );

            if (VideoData == null)
            {
                return NotFound("O vídeo solicitado não foi encontrado.");
            }

            if (VideoData.Status != VideoStatus.Available)
            {
                // Se o vídeo ainda está processando ou deu erro, não exibe.
                return Page(); // Retorna a página, que pode mostrar uma mensagem de status.
            }

            // Se tudo estiver OK, permite que a página seja renderizada
            return Page();
        }
    }
}
