using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Models
{
    public class Course
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Propriedade de navegação para a lista de vídeos que pertencem a este curso
        public virtual ICollection<Video> Videos { get; set; } = new List<Video>();
    }
}
