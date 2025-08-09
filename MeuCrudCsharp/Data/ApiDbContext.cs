using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Data
{
    /// <summary>
    /// Contexto de dados da aplicação baseado em ASP.NET Identity.
    /// Expõe os conjuntos de entidades e integra com o Identity para autenticação e usuários.
    /// </summary>
    public class ApiDbContext : IdentityDbContext<Users>
    {
        /// <summary>
        /// Inicializa uma nova instância do contexto com as opções fornecidas.
        /// </summary>
        /// <param name="options">Opções de configuração do Entity Framework Core.</param>
        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options) { }

        /// <summary>
        /// Conjunto de entidades de pagamentos avulsos.
        /// </summary>
        public DbSet<Payments> Payments { get; set; }

        /// <summary>
        /// Conjunto de entidades de vídeos associados aos cursos.
        /// </summary>
        public DbSet<Video> Videos { get; set; }

        /// <summary>
        /// Conjunto de entidades de cursos.
        /// </summary>
        public DbSet<Course> Courses { get; set; }

        /// <summary>
        /// Conjunto de entidades de assinaturas de planos.
        /// </summary>
        public DbSet<Subscription> Subscriptions { get; set; }

        /// <summary>
        /// Conjunto de entidades de planos de assinatura.
        /// </summary>
        public DbSet<Plan> Plans { get; set; }
    }
}
