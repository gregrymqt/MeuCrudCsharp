using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    // Tabela para Seções de Texto Genéricas
    [Table("AboutSections")]
    public class AboutSection
    {
        [Key]
        public int Id { get; set; }

        // Pode ser útil para ordenar as seções na tela
        public int OrderIndex { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty; // Ref: AboutSectionDto [cite: 20]

        public string Description { get; set; } = string.Empty; // Pode ser HTML ou Texto longo

        public string ImageUrl { get; set; } = string.Empty; // URL vinda do upload

        public int? FileId { get; set; } // FK opcional para rastrear o arquivo

        public string ImageAlt { get; set; } = string.Empty;
    }

    // Tabela para Membros da Equipe
    [Table("AboutTeamMembers")]
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // Ref: TeamMemberDto [cite: 30]

        [MaxLength(100)]
        public string Role { get; set; } = string.Empty;

        public string PhotoUrl { get; set; } = string.Empty; // URL vinda do upload

        public int? FileId { get; set; } // FK opcional para rastrear o arquivo

        [MaxLength(200)]
        public string? LinkedinUrl { get; set; } // Nullable [cite: 33]

        [MaxLength(200)]
        public string? GithubUrl { get; set; } // Nullable [cite: 34]
    }
}
