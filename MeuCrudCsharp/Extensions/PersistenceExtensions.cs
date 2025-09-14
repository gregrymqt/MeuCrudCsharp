using Hangfire;
using Hangfire.Redis.StackExchange;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Configura os serviços de persistência de dados, incluindo o banco de dados principal (SQL Server),
    /// o ASP.NET Core Identity, o cache distribuído (Redis ou em memória) e o Hangfire para background jobs.
    /// </summary>
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        // --- 1. Configuração do Banco de Dados Principal (SQL Server) e Identity ---
        builder.Services.AddDbContextFactory<ApiDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddIdentity<Users, IdentityRole>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
        })
        .AddEntityFrameworkStores<ApiDbContext>()
        .AddDefaultTokenProviders();

        // --- 2. Configuração Condicional de Cache e Hangfire (Redis vs. Em Memória) ---
        var useRedis = builder.Configuration.GetValue<bool>("USE_REDIS");
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

        if (useRedis)
        {
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "A variável de ambiente USE_REDIS está como 'true', mas a string de conexão do Redis não foi encontrada.");
            }
            
            builder.AddRedisPersistence(redisConnectionString);
        }
        else
        {
            builder.AddInMemoryPersistence();
        }

        return builder;
    }

    /// <summary>
    /// Configura os serviços para usar Redis como provedor de cache e Hangfire.
    /// </summary>
    private static void AddRedisPersistence(this WebApplicationBuilder builder, string redisConnectionString)
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "MeuApp_"; // Prefixo para as chaves de cache
        });

        builder.Services.AddHangfire(config =>
            config.UseRedisStorage(redisConnectionString));
        
        builder.Services.AddHangfireServer();
        Console.WriteLine("--> Usando Redis para Cache Distribuído e Hangfire.");
    }
    
    /// <summary>
    /// Configura os serviços para usar a memória interna como provedor de cache e Hangfire (ideal para desenvolvimento).
    /// </summary>
    private static void AddInMemoryPersistence(this WebApplicationBuilder builder)
    {
        builder.Services.AddDistributedMemoryCache();
        
        builder.Services.AddHangfire(config =>
            config.UseInMemoryStorage());
        
        builder.Services.AddHangfireServer();
        Console.WriteLine("--> Usando Cache em Memória para Cache Distribuído e Hangfire.");
    }
}
