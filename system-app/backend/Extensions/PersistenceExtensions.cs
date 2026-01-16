using Hangfire;
using Hangfire.Redis.StackExchange;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models; // Necessário para acessar a classe Roles
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

public static class PersistenceExtensions
{
    public static WebApplicationBuilder AddPersistence(this WebApplicationBuilder builder)
    {
        // --- 1. Configuração do Banco de Dados Principal (SQL Server) ---
        builder.Services.AddDbContextFactory<ApiDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
        );

        // --- CORREÇÃO AQUI ---
        // Alterado de <Users, IdentityRole> para <Users, Roles>
        builder
            .Services.AddIdentity<Users, Roles>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;

                // Configurações opcionais úteis para teste (remova se não quiser)
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApiDbContext>()
            .AddDefaultTokenProviders();

        // --- 2. Configuração do MongoDB (NOVO) ---
        // Aqui configuramos para que qualquer Service possa pedir um IMongoDatabase
        builder.Services.AddSingleton<IMongoClient>(sp =>
        {
            var mongoConnString = builder.Configuration.GetConnectionString("MongoConnection");
            return new MongoClient(mongoConnString);
        });

        builder.Services.AddScoped<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            // Defina o nome do banco aqui (ou pegue do appsettings se preferir)
            return client.GetDatabase("MeuCrudSupportDb");
        });

        // --- 3. Configuração Condicional de Cache e Hangfire ---
        var useRedis = builder.Configuration.GetValue<bool>("USE_REDIS");
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

        if (useRedis)
        {
            if (string.IsNullOrEmpty(redisConnectionString))
            {
                throw new InvalidOperationException(
                    "A variável USE_REDIS é true mas a connection string está vazia."
                );
            }
            builder.AddRedisPersistence(redisConnectionString);
        }
        else
        {
            builder.AddInMemoryPersistence();
        }

        return builder;
    }

    // ... Os métodos AddRedisPersistence e AddInMemoryPersistence continuam iguais ...
    private static void AddRedisPersistence(
        this WebApplicationBuilder builder,
        string redisConnectionString
    )
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "MeuApp_";
        });

        builder.Services.AddHangfire(config => config.UseRedisStorage(redisConnectionString));
        builder.Services.AddHangfireServer();
        Console.WriteLine("--> Usando Redis para Cache Distribuído e Hangfire.");
    }

    private static void AddInMemoryPersistence(this WebApplicationBuilder builder)
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddHangfire(config => config.UseInMemoryStorage());
        builder.Services.AddHangfireServer();
        Console.WriteLine("--> Usando Cache em Memória para Cache Distribuído e Hangfire.");
    }
}
