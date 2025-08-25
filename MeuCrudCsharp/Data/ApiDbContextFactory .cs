using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

// Substitua "Seu.Namespace.Aqui" pelo namespace real do seu projeto
namespace MeuCrudCsharp.Data
{
    public class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
    {
        public ApiDbContext CreateDbContext(string[] args)
        {
            // Constrói a configuração para ler o appsettings.json
            // Isso permite que a ferramenta 'dotnet ef' encontre sua connection string
            // sem precisar rodar todo o Program.cs
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true) // Opcional, mas bom ter
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();

            // Pega a connection string diretamente da configuração
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Configura o DbContext para usar SQL Server
            optionsBuilder.UseSqlServer(connectionString);

            return new ApiDbContext(optionsBuilder.Options);
        }
    }
}
