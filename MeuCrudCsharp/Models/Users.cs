using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MeuCrudCsharp.Models
{
    // A classe herda todas as propriedades padrão do Identity (Id, Email, UserName, etc.)
    public class Users : IdentityUser
    {
        // --- Mantenha APENAS as suas propriedades customizadas ---

        [Required]
        public string? GoogleId { get; set; }

        [Required]
        [MaxLength(250)]
        public string? Name { get; set; } // Um nome de exibição, diferente do UserName

        [MaxLength(500)]
        public string? AvatarUrl { get; set; } // Tornando a URL do avatar opcional

        // A propriedade de navegação está correta
        public virtual Payment_User? Payment_User { get; set; }
    }
}
