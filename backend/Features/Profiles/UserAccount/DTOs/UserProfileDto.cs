namespace MeuCrudCsharp.Features.Profiles.UserAccount.DTOs
{
    public class UserProfileDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
    }

    // DTO específico para resposta de upload, se preferir separar
    public class AvatarUpdateResponse
    {
        public string AvatarUrl { get; set; }
        public string Message { get; set; }
    }
}
