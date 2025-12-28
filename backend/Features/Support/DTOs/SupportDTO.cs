using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Support.DTOs
{
    // O que o usuário envia para criar (já tínhamos falado dessa, ajustei para DTO)
    public class CreateSupportTicketDto
    {
        [Required(ErrorMessage = "O contexto é obrigatório.")]
        [MinLength(3, ErrorMessage = "O contexto deve ter no mínimo 3 caracteres.")]
        public string Context { get; set; }

        [Required(ErrorMessage = "A explicação é obrigatória.")]
        [MinLength(10, ErrorMessage = "A explicação deve ter no mínimo 10 caracteres.")]
        public string Explanation { get; set; }
    }

    // O que o Admin envia para atualizar o status
    public class UpdateSupportTicketDto
    {
        [Required]
        public string Status { get; set; } // "Open", "InProgress", "Closed"
    }

    // O que a API devolve para o Front (Response)
    public class SupportTicketResponseDto
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Context { get; set; }
        public string Explanation { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
