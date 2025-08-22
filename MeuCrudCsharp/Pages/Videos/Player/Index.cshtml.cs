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

        // ✅ MUDANÇA 1: A propriedade agora é do tipo Guid para receber o PublicId
        [BindProperty(SupportsGet = true)]
        public Guid VideoId { get; set; }

        public Video? VideoData { get; private set; }
        public Users? UserProfile { get; private set; }

        public IndexModel(ApiDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // --- ETAPA 1: Validar o Usuário (Permanece igual) ---
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            UserProfile = await _context.Users
                .Include(u => u.Payments)
                .FirstOrDefaultAsync(u => u.Id == userIdString);

            if (UserProfile == null || UserProfile.Payments.Any(p => p.Status == "rejected"))
            {
                return Forbid();
            }

            // --- ETAPA 2: Validar e Buscar o Vídeo ---
            if (VideoId == Guid.Empty)
            {
                return NotFound("O ID do vídeo não foi fornecido ou é inválido.");
            }

            // ✅ MUDANÇA 2: A busca no banco agora é feita pelo PublicId
            VideoData = await _context.Videos
                .Include(v => v.Course) // Incluindo o curso para exibir o nome
                .FirstOrDefaultAsync(v => v.PublicId == VideoId);

            if (VideoData == null)
            {
                return NotFound("O vídeo solicitado não foi encontrado.");
            }

            if (VideoData.Status != VideoStatus.Available)
            {
                return Page();
            }

            return Page();
        }
    }
}