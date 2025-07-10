using MeuCrudCsharp.Models; // Importa a classe Produto
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
        {
        }

        // Esta linha cria uma tabela chamada "Produtos" baseada no modelo "Produto"
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Users> Users { get; set; }
        public DbSet<Payment_User> Payment_User { get; set; }


    }
}