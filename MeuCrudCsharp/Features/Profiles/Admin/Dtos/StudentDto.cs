using System;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    public class StudentDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }

        // --- MUDANÇAS AQUI ---

        // O status da assinatura (ex: "ativo", "cancelado", "pausado")
        public string? SubscriptionStatus { get; set; }

        // O nome do plano que ele assinou (ex: "Plano Anual Premium")
        public string? PlanName { get; set; }

        public DateTime RegistrationDate { get; set; }
    }
}
