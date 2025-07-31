namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string SubscriptionStatus { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
