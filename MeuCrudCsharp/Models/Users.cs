using System; // Necessário para usar o tipo Guid
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    public class Users
    {
        [Key] // Define explicitamente esta propriedade como a chave primária.
        public Guid Id { get; set; }

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

       
        public Users()
        {
            Id = Guid.NewGuid();
        }
    }
}