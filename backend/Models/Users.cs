using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(GoogleId), IsUnique = true, Name = "IX_Users_GoogleId")]
    public class Users : IdentityUser
    {
        public Guid PublicId { get; set; } = Guid.NewGuid();
        public string? Name { get; set; }

        // --- IMAGENS ---
        public int? AvatarFileId { get; set; }

        // ADICIONE ISTO AQUI PARA CORRIGIR O ERRO CS0117 e CS1061:
        public string? AvatarUrl { get; set; }

        // --- Autenticação Externa ---
        public string? GoogleId { get; set; }
        public string? CustomerId { get; set; }

        // --- Relacionamentos ---
        public virtual Subscription? Subscription { get; set; }
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();
    }
}
