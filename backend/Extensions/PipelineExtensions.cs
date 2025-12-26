using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MeuCrudCsharp.Extensions;

public static class PipelineExtensions
{
    /// <summary>
    /// Configura o pipeline de requisições HTTP (middlewares) e endpoints da aplicação.
    /// Também executa tarefas de inicialização, como o seeding de roles no banco de dados.
    /// </summary>
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

        // --- 3. Roteamento e CORS ---
        app.UseRouting();
        app.UseCors(WebServicesExtensions.CorsPolicyName);

        // --- 4. Autenticação e Autorização ---
        // AQUI ESTAVA A REDUNDÂNCIA:
        // Substituímos a configuração manual pela chamada ao método do AuthExtensions
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

    /// <summary>
    /// Cria as roles definidas em AppRoles no banco de dados durante a inicialização.
    /// </summary>
    private static async Task SeedInitialRoles(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        // CORREÇÃO AQUI: Mudamos de RoleManager<IdentityRole> para RoleManager<Roles>
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Roles>>();

        string[] roles = { AppRoles.Admin, AppRoles.User, AppRoles.Manager };

        foreach (var roleName in roles)
        {
            // Verifica se a role existe
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                // CORREÇÃO AQUI: Instanciamos 'Roles' em vez de 'IdentityRole'
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
}
