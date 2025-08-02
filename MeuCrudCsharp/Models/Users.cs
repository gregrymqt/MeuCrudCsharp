using System.Collections.Generic; // Adicione se não tiver
using MercadoPago.Resource.Payment;
using Microsoft.AspNetCore.Identity;

namespace MeuCrudCsharp.Models
{
    public class Users : IdentityUser
    {
        // ... suas propriedades existentes como Name, AvatarUrl, etc.
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } // Recomendado ter uma data de criação

        public virtual Subscription? Subscription { get; set; }

        // Um usuário pode ter vários pagamentos avulsos
        public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();

        public string? GoogleId { get; set; } // Adiciona o campo GoogleId para autenticação via Google
    }
}
