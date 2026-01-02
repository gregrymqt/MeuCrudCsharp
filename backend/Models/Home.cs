using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    // Tabela para o Carrossel
    [Table("HomeHeroes")]
    public class HomeHero
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // Ref: HeroSlideDto [cite: 6]
        
        [MaxLength(200)]
        public string Subtitle { get; set; } = string.Empty;

        // Aqui você guarda o caminho final, ex: "/uploads/home/banner1.jpg"
        // Esse caminho vem da lógica do seu EntityFile.
        public string ImageUrl { get; set; } = string.Empty; 

        [MaxLength(50)]
        public string ActionText { get; set; } = string.Empty; // Texto do botão
        
        [MaxLength(500)]
        public string ActionUrl { get; set; } = string.Empty;  // Link do botão
    }

    // Tabela para os Serviços
    [Table("HomeServices")]
    public class HomeService
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string IconClass { get; set; } = string.Empty; // Ref: ServiceDto [cite: 11]

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        // Se precisar de botão nos serviços também:
        public string ActionText { get; set; } = string.Empty;
        public string ActionUrl { get; set; } = string.Empty;
    }
}