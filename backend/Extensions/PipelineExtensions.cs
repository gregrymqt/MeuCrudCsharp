using Hangfire;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MeuCrudCsharp.Extensions;

public static class PipelineExtensions
{
    /// <summary>
    /// Configura o pipeline de requisições HTTP (middlewares) e endpoints da aplicação.
    /// Também executa tarefas de inicialização, como o seeding de roles no banco de dados.
    /// </summary>
    public static async Task<WebApplication> UseAppPipeline(this WebApplication app)
    {
        // --- 1. Middlewares de Ambiente e Segurança ---
        app.UseForwardedHeaders(); // Para confiança em proxies reversos (Ngrok, IIS, etc.)
        app.UseCookiePolicy();

        // Middlewares de desenvolvimento (Swagger)
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // --- 2. Tarefas de Inicialização (Seeding) ---
        // Cria as roles iniciais (Admin, User, etc.) se não existirem.
        await ApplyMigrations(app);

        // Cria as roles iniciais (Admin, User, etc.) se não existirem.
        await SeedInitialRoles(app);

        // --- 3. Middlewares de Requisição ---
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors(WebServicesExtensions.CorsPolicyName); // Usa a constante definida anteriormente
        app.UseRouting();

        // A ordem é crucial: Autenticação primeiro, depois Autorização.
        app.UseAuthentication();
        app.UseAuthorization();

        // Adiciona o dashboard do Hangfire.
        app.UseHangfireDashboard();

        // --- 4. Mapeamento de Endpoints ---
        // Mapeia os hubs do SignalR.
        app.MapHub<VideoProcessingHub>("/videoProcessingHub");
        app.MapHub<RefundProcessingHub>("/RefundProcessingHub");
        app.MapHub<PaymentProcessingHub>("/PaymentProcessingHub");

        // Mapeia Razor Pages e Controllers.
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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = { AppRoles.Admin, AppRoles.User, AppRoles.Manager };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    /// <summary>
    /// Aplica automaticamente as migrations pendentes do Entity Framework Core na inicialização.
    /// </summary>
    private static async Task ApplyMigrations(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            Console.WriteLine("--> Aplicando migrations do banco de dados...");
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"--> Erro ao aplicar migrations: {ex.Message}");
            // Opcional: Lançar a exceção pode ser útil para interromper a inicialização em caso de falha crítica no DB.
        }
    }
}
