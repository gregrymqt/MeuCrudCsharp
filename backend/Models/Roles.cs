using Microsoft.AspNetCore.Identity;

namespace MeuCrudCsharp.Models
{
    // Criando a Model Roles herdando de IdentityRole para integração nativa
    public class Roles : IdentityRole
    {
        // Você pode adicionar propriedades extras aqui se precisar, 
        // ex: Description, CreatedAt, etc.
        public Roles() : base() { }
        public Roles(string roleName) : base(roleName) { }
    }
}