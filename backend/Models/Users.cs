using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    // Mantendo a herança do IdentityUser
    [Index(nameof(GoogleId), IsUnique = true, Name = "IX_Users_GoogleId")]
    public class Users : IdentityUser
    {
        public Guid PublicId { get; set; } = Guid.NewGuid(); // 
        public string? Name { get; set; } // 
        public string? AvatarUrl { get; set; } // 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // 

        // --- Autenticação Externa ---
        public string? GoogleId { get; set; } // 
        public string? CustomerId { get; set; } // (Stripe/MercadoPago ID)

        // --- Relacionamentos ---
        // Relacionamento 1:1 ou 1:N com Assinatura (Depende da sua regra, aqui pus 1:1 opcional)
        public virtual Subscription? Subscription { get; set; } // 

        // Relacionamento 1:N com Pagamentos
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>(); // 
    }
}