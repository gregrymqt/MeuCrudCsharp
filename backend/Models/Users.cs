using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(GoogleId), IsUnique = true, Name = "IX_Users_GoogleId")]
    public class Users : IdentityUser
    {
        // ... suas propriedades existentes como Name, AvatarUrl, etc.

        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } // Recomendado ter uma data de criação
        public virtual Subscription? Subscription { get; set; }

        // Um usuário pode ter vários pagamentos avulsos
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();
        public string? GoogleId { get; set; } // Adiciona o campo GoogleId para autenticação via Google
        public string? CustomerId { get; set; } // Adiciona o campo CustomerId para autenticação via Google
    }
}
