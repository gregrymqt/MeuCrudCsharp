using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Jobs;

namespace MeuCrudCsharp.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Configura a injeção de dependência para os serviços da aplicação.
    /// </summary>
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        // Usa o Scrutor para registrar automaticamente todos os serviços que seguem o padrão de nomenclatura.
        builder.Services.Scan(scan => scan
            // Escaneia o assembly principal do projeto.
            .FromEntryAssembly()
            // Adiciona todas as classes dos namespaces especificados.
            .AddClasses(classes => classes.InNamespaces(
                "MeuCrudCsharp.Features.Courses.Services", 
                "MeuCrudCsharp.Features.Emails.Services",
                "MeuCrudCsharp.Features.MercadoPago.Payments.Services",
                "MeuCrudCsharp.Features.MercadoPago.Notification.Services",
                "MeuCrudCsharp.Features.MercadoPago.Plans.Services",
                "MeuCrudCsharp.Features.MercadoPago.Clients.Services",
                "MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services",
                "MeuCrudCsharp.Features.MercadoPago.Refunds.Services",
                "MeuCrudCsharp.Features.MercadoPago.Refunds.Notifications",
                "MeuCrudCsharp.Features.Profiles.Admin.Services",
                "MeuCrudCsharp.Features.Profiles.UserAccount.Services",
                "MeuCrudCsharp.Features.Videos.Services",
                "MeuCrudCsharp.Features.Videos.Notification",
                "MeuCrudCsharp.Features.Caching.Services",
                "MeuCrudCsharp.Features.Authorization",
                "MeuCrudCsharp.Features.Auth",
                "MeuCrudCsharp.Features.Shared.User",
                "MeuCrudCsharp.AppSettings"
            ))
            // Registra as classes como implementações de suas interfaces.
            .AsImplementedInterfaces()
            // Define o tempo de vida como "Scoped" (uma instância por requisição HTTP).
            .WithScopedLifetime());

        // Registra manualmente serviços que não seguem o padrão ou precisam de configuração especial.
        builder.Services.AddScoped<ProcessPaymentNotificationJob>();
        builder.Services.AddScoped<IQueueService, BackgroundJobQueueService>();
        builder.Services.Configure<GeneralSettings>(builder.Configuration.GetSection(GeneralSettings.SectionName));
        builder.Services.Configure<MercadoPagoSettings>(
            builder.Configuration.GetSection(MercadoPagoSettings.SectionName));
        builder.Services.Configure<SendGridSettings>(builder.Configuration.GetSection(SendGridSettings.SectionName));
        builder.Services.Configure<GoogleSettings>(builder.Configuration.GetSection(GoogleSettings.SectionName));
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
        builder.Services.AddSingleton<ConnectionMapping<string>>();

        
        return builder;
    }
}