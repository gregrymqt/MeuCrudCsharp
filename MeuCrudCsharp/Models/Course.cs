using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Models
{
    [Index(nameof(Name), IsUnique = true)]
    public class Course
    {
        [Key]
        public int Id { get; set; }

        public Guid PublicId { get; set; } = new Guid();

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        // Propriedade de navegação para a lista de vídeos que pertencem a este curso
        public virtual ICollection<Video> Videos { get; set; } = new List<Video>();
    }
}
