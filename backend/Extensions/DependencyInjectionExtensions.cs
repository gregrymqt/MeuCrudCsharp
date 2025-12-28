using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Jobs;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Job;
using MeuCrudCsharp.Features.MercadoPago.Jobs.Services;

// ... (mantenha os outros usings necessários)

namespace MeuCrudCsharp.Extensions;

public static class DependencyInjectionExtensions
{
    public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
    {
        builder.Services.Scan(scan =>
            scan.FromEntryAssembly()
                .AddClasses(classes =>
                    classes.InNamespaces(
                        // --- 1. Features com Repositórios (CRÍTICO: Adicionados os Repositories) ---
                        // Courses
                        "MeuCrudCsharp.Features.Courses.Services",
                        "MeuCrudCsharp.Features.Courses.Repositories",
                        // Support (Feature Nova)
                        "MeuCrudCsharp.Features.Support.Services",
                        "MeuCrudCsharp.Features.Support.Repositories",
                        // Admin Profile
                        "MeuCrudCsharp.Features.Profiles.Admin.Services",
                        "MeuCrudCsharp.Features.Profiles.Admin.Repositories",
                        // --- 2. Features Gerais (Baseado nas Imagens) ---
                        "MeuCrudCsharp.Features.Auth.Services",
                        "MeuCrudCsharp.Features.Auth.Repositories",
                        "MeuCrudCsharp.Features.Files.Services",
                        "MeuCrudCsharp.Features.Files.Repositories",
                        "MeuCrudCsharp.Features.Profiles.UserAccount.Services",
                        "MeuCrudCsharp.Features.Profiles.UserAccount.Repositories",
                        "MeuCrudCsharp.Features.Profiles.Admin.Services",
                        "MeuCrudCsharp.Features.Profiles.Admin.Repositories",
                        "MeuCrudCsharp.Features.Videos.Services",
                        "MeuCrudCsharp.Features.Videos.Repositories",
                        "MeuCrudCsharp.Features.Videos.Notification",
                        // Essenciais (Caching e Emails geralmente têm a implementação dentro de .Services)
                        "MeuCrudCsharp.Features.Caching.Services",
                        "MeuCrudCsharp.Features.Emails.Services",
                        // --- 3. Mercado Pago (Sub-features) ---
                        "MeuCrudCsharp.Features.MercadoPago.Payments.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Payments.Repositories",
                        "MeuCrudCsharp.Features.MercadoPago.Notification.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Plans.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Plans.Repositories",
                        "MeuCrudCsharp.Features.MercadoPago.Clients.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Subscriptions.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Subscriptions.Repositories",
                        "MeuCrudCsharp.Features.MercadoPago.Refunds.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Refunds.Notifications",
                        "MeuCrudCsharp.Features.MercadoPago.Jobs.Services",
                        "MeuCrudCsharp.Features.MercadoPago.WebHooks.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Chargebacks.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Chargebacks.Notifications",
                        "MeuCrudCsharp.Features.MercadoPago.Claims.Services",
                        "MeuCrudCsharp.Features.MercadoPago.Claims.Repositories",
                        "MeuCrudCsharp.Features.MercadoPago.Hub",
                        // --- 4. Configurações Globais ---
                        "MeuCrudCsharp.AppSettings"
                    )
                )
                .AsImplementedInterfaces()
                .WithScopedLifetime()
        );

        // --- Configurações Manuais (Jobs e Settings) ---
        // Estes não entram no Scan automáticos pois precisam de config específica ou são Jobs
        builder.Services.AddScoped<ProcessPaymentNotificationJob>();

        builder.Services.Configure<GeneralSettings>(
            builder.Configuration.GetSection(GeneralSettings.SectionName)
        );

        builder.Services.Configure<MercadoPagoSettings>(
            builder.Configuration.GetSection(MercadoPagoSettings.SectionName)
        );

        builder.Services.Configure<SendGridSettings>(
            builder.Configuration.GetSection(SendGridSettings.SectionName)
        );

        builder.Services.Configure<GoogleSettings>(
            builder.Configuration.GetSection(GoogleSettings.SectionName)
        );

        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

        builder.Services.AddSingleton<ConnectionMapping<string>>();

        builder.Services.Configure<FFmpegSettings>(
            builder.Configuration.GetSection("FFmpegSettings")
        );

        return builder;
    }
}
