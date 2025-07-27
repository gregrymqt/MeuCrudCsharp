using System.Security.Claims;
using System.Threading.Tasks;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Payments.ViewModels; // Supondo o novo namespace
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class ReceiptModel : PageModel
{
    private readonly ApiDbContext _context;

    public ReceiptModel(ApiDbContext context)
    {
        _context = context;
    }

    // A propriedade para guardar os dados que ser�o exibidos na p�gina
    [BindProperty]
    public ReceiptViewModel Receipt { get; set; }

    public async Task<IActionResult> OnGetAsync(string paymentId)
    {
        if (string.IsNullOrEmpty(paymentId))
        {
            return NotFound();
        }

        // Pega o ID do usu�rio logado para seguran�a
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Busca o pagamento no banco de dados, garantindo que ele pertence ao usu�rio logado
        var payment = await _context
            .Payment_User.Include(p => p.User) // Inclui os dados do usu�rio na consulta
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId && p.UserId.ToString() == userId);

        if (payment == null || payment.Status != "approved")
        {
            // Se o pagamento n�o existe, n�o pertence ao usu�rio ou n�o foi aprovado, n�o mostra.
            return Forbid();
        }

        // Preenche o ViewModel com os dados do banco
        Receipt = new ReceiptViewModel
        {
            CompanyName = "Nome da Sua Empresa",
            CompanyCnpj = "XX.XXX.XXX/0001-XX",
            PaymentId = payment.PaymentId,
            PaymentDate = payment.DateApproved ?? System.DateTime.Now, // Usa a data de aprova��o
            CustomerName = payment.User.Name,
            CustomerCpf = payment.CustomerCpf, // Voc� buscaria isso do objeto do MP que salvou no DB
            Amount = payment.Amount,
            PaymentMethod = $"Cart�o de Cr�dito final {payment.LastFourDigits}",
        };

        return Page();
    }
}
