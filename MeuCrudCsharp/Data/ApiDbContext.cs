using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity; // Adicione este
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore; // Adicione este

namespace MeuCrudCsharp.Data
{
    public class ApiDbContext : IdentityDbContext<Users> // Herda de IdentityDbContext
    {
        // O construtor precisa passar as 'options' para a classe base
        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options) { }

        // Esta linha cria uma tabela chamada "Produtos" baseada no modelo "Produto"
        public DbSet<Payments> Payments { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Users> User { get; set; } // Adiciona a tabela de usuários
    }
}
