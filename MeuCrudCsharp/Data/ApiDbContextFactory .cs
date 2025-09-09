using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace MeuCrudCsharp.Data
{
    public class ApiDbContextFactory : IDesignTimeDbContextFactory<ApiDbContext>
    {
        public ApiDbContext CreateDbContext(string[] args)
        {
            // Detecta o ambiente atual (Development, Production, etc.)
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables();

            // A MÁGICA ACONTECE AQUI!
            // Se estivermos em desenvolvimento, carregue os User Secrets.
            if (environment == "Development")
            {
                builder.AddUserSecrets<ApiDbContextFactory>();
            }

            IConfigurationRoot configuration = builder.Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApiDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("A ConnectionString 'DefaultConnection' não foi encontrada. Verifique seus arquivos appsettings.json e User Secrets.");
            }

            optionsBuilder.UseSqlServer(connectionString);

            return new ApiDbContext(optionsBuilder.Options);
        }
    }
}