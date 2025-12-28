using System.Reflection;
using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Documents.Attributes; // <--- CERTIFIQUE-SE QUE ESTE USING APONTA PARA ONDE CRIOU O ATRIBUTO
using MeuCrudCsharp.Documents.Interfaces; // Certifique-se que IMongoDocument está aqui
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MeuCrudCsharp.Extensions;

public static class PipelineExtensions
{
    public static async Task<WebApplication> UseAppPipeline(this WebApplication app)
    {
        // --- 1. Configuração de Erros e Segurança Básica ---
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();

        // --- 2. Inicialização do Banco de Dados ---
        await ApplyMigrations(app);
        await SeedInitialRoles(app);

        // NOVO: Chama a configuração dos índices do Mongo
        await ConfigureMongoDbIndexes(app);

        // --- 3. Roteamento e CORS ---
        app.UseRouting();
        app.UseCors(WebServicesExtensions.CorsPolicyName);

        // --- 4. Autenticação e Autorização ---
        app.UseAuthFeatures();

        // --- 5. Middlewares Específicos ---
        app.UseHangfireDashboard();

        // --- 6. Endpoints ---
        app.MapHub<VideoProcessingHub>("/videoProcessingHub");
        app.MapHub<RefundProcessingHub>("/RefundProcessingHub");
        app.MapHub<PaymentProcessingHub>("/PaymentProcessingHub");

        app.MapRazorPages();
        app.MapControllers();

        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

        return app;
    }

    private static async Task SeedInitialRoles(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Roles>>();

        string[] roles = { AppRoles.Admin, AppRoles.User, AppRoles.Manager };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new Roles(roleName));
            }
        }
    }

    private static async Task ApplyMigrations(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            if ((await dbContext.Database.GetPendingMigrationsAsync()).Any())
            {
                await dbContext.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Erro ao aplicar migrations: {ex.Message}");
        }
    }

    private static async Task ConfigureMongoDbIndexes(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var database = scope.ServiceProvider.GetService<IMongoDatabase>();

        // Se o Mongo não estiver configurado, ignora
        if (database == null)
            return;

        // 1. Pega todas as classes que implementam IMongoDocument
        var documentTypes = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p =>
                typeof(IMongoDocument).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract
            );

        foreach (var docType in documentTypes)
        {
            // 2. Tenta pegar o nome da coleção via propriedade estática "CollectionName"
            var collectionNameProperty = docType.GetProperty(
                "CollectionName",
                BindingFlags.Public | BindingFlags.Static
            );
            var collectionName = collectionNameProperty?.GetValue(null) as string;

            // Se a classe não definiu um nome, pulamos (ou usamos uma lógica padrão se preferir)
            if (string.IsNullOrEmpty(collectionName))
                continue;

            // --- CORREÇÃO DO ERRO CS0103 ---
            // A variável 'collection' precisa ser criada aqui para ser usada dentro do próximo loop
            var collection = database.GetCollection<BsonDocument>(collectionName);

            // 3. Pega todas as propriedades marcadas com [MongoIndex]
            var propertiesToIndex = docType
                .GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(MongoIndexAttribute)));

            foreach (var prop in propertiesToIndex)
            {
                var attribute = prop.GetCustomAttribute<MongoIndexAttribute>();

                // Pega o nome do campo no Bson (se existir), senão usa o nome da propriedade
                var bsonElement =
                    prop.GetCustomAttribute<MongoDB.Bson.Serialization.Attributes.BsonElementAttribute>();
                var fieldName = bsonElement?.ElementName ?? prop.Name;

                try
                {
                    var indexKeys = attribute.Descending
                        ? Builders<BsonDocument>.IndexKeys.Descending(fieldName)
                        : Builders<BsonDocument>.IndexKeys.Ascending(fieldName);

                    var indexOptions = new CreateIndexOptions { Unique = attribute.Unique };
                    var indexModel = new CreateIndexModel<BsonDocument>(indexKeys, indexOptions);

                    // Agora a variável 'collection' existe e pode ser usada
                    await collection.Indexes.CreateOneAsync(indexModel);
                    Console.WriteLine(
                        $"--> [Mongo] Index criado em '{collectionName}': {fieldName}"
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"--> [Mongo] Erro ao criar index em '{collectionName}': {ex.Message}"
                    );
                }
            }
        }
    }
}
