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

        /// <summary>
        /// Conjunto de entidades referentes a notificação de claims.
        /// </summary>
        public DbSet<Claims> Claims { get; set; }

        /// <summary>
        /// Conjunto de entidades referentes a notificação de Chargeback.
        /// </summary>
        public DbSet<Chargeback> Chargebacks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // É muito importante chamar o método base primeiro!
            base.OnModelCreating(modelBuilder);

            // Configuração da relação Usuário -> Pagamentos
            // Um usuário pode ter muitos pagamentos. Se o usuário for deletado,
            // seus pagamentos também serão (comportamento padrão Cascade).
            modelBuilder
                .Entity<Payments>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId);

            // Configuração da relação Pagamento -> Assinatura
            // Esta é a configuração CRÍTICA que resolve o erro de múltiplos caminhos em cascata.
            // Se uma Assinatura for deletada, o banco de dados NÃO permitirá a exclusão
            // se houver algum Pagamento associado a ela.
            modelBuilder
                .Entity<Payments>()
                .HasOne(p => p.Subscription) // Um pagamento tem uma assinatura
                .WithMany() // Uma assinatura pode ter muitos pagamentos (não há propriedade de coleção em Subscription)
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict); // <-- AQUI ESTÁ A SOLUÇÃO!

            // Configura a propriedade FrequencyType da entidade Plan
            modelBuilder.Entity<Plan>().Property(p => p.FrequencyType).HasConversion<string>(); // <-- Mágica acontece aqui!
        }
    }
}
