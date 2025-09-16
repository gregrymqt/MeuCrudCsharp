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
                "MeuCrudCsharp.Features.Clients.Service",
                "MeuCrudCsharp.Features.Courses.Services", // Corrigido: Estava no singular "Course"
                "MeuCrudCsharp.Features.Emails.Services",
                "MeuCrudCsharp.Features.MercadoPago.Payments.Services",
                "MeuCrudCsharp.Features.MercadoPago.Notification.Services",
                "MeuCrudCsharp.Features.Plans.Services",
                "MeuCrudCsharp.Features.Profiles.Admin.Services",
                "MeuCrudCsharp.Features.Profiles.UserAccount.Services",
                "MeuCrudCsharp.Features.Refunds.Services",
                "MeuCrudCsharp.Features.Refunds.Notifications",
                "MeuCrudCsharp.Features.Subscriptions.Services",
                "MeuCrudCsharp.Features.Videos.Service",
                "MeuCrudCsharp.Features.Videos.Services",
                "MeuCrudCsharp.Features.Caching",
                "MeuCrudCsharp.Features.Authorization",
                "MeuCrudCsharp.Features.Auth"
            ))
            // Registra as classes como implementações de suas interfaces.
            .AsImplementedInterfaces()
            // Define o tempo de vida como "Scoped" (uma instância por requisição HTTP).
            .WithScopedLifetime());

        // Registra manualmente serviços que não seguem o padrão ou precisam de configuração especial.
        builder.Services.AddScoped<ProcessPaymentNotificationJob>();
        builder.Services.AddScoped<IQueueService, BackgroundJobQueueService>();

        return builder;
    }
}