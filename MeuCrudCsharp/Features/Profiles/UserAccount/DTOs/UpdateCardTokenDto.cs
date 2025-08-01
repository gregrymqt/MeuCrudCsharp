using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.DTOs
{
    public class UpdateCardTokenDto
    {
        [Required]
        public string NewCardToken { get; set; }
    }
}
