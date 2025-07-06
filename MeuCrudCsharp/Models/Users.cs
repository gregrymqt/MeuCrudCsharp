// Em Models/ApplicationUser.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    public class Users
    {
        public int Id { get; set; } // Chave primária do seu banco

        [Required]
        public string GoogleId { get; set; } // O ID do Google é uma string, não um número.

        [Required]
        [MaxLength(250)]
        public string Name { get; set; }

        [Required]
        [MaxLength(250)]
        public string Email { get; set; }

        [MaxLength(500)]
        public string AvatarUrl { get; set; }

        // REMOVEMOS A PROPRIEDADE DE SENHA. NÃO PRECISAMOS DELA!
    }
}