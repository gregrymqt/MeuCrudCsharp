namespace MeuCrudCsharp.Features.Auth.Dtos
{
    public class UserSessionDto
    {
        public Guid PublicId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new(); // Adicione isso

        // Novos campos booleanos leves (substituindo os objetos completos)
        public bool HasActiveSubscription { get; set; }
        public bool HasPaymentHistory { get; set; }
    }
}