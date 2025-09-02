using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hangfire;
using Hangfire.Redis.StackExchange;
using MercadoPago.Config;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth;
using MeuCrudCsharp.Features.Authorization;
using MeuCrudCsharp.Features.Clients.Interfaces;
using MeuCrudCsharp.Features.Clients.Service;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Courses.Services;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.Services;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Jobs; // Adicionado para os Jobs
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Notification;
using MeuCrudCsharp.Features.MercadoPago.Payments.Services;
using MeuCrudCsharp.Features.Plans.Interfaces;
using MeuCrudCsharp.Features.Plans.Services;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Profiles.Admin.Services;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.Services;
using MeuCrudCsharp.Features.Refunds.Interfaces;
using MeuCrudCsharp.Features.Refunds.Notifications;
using MeuCrudCsharp.Features.Refunds.Services;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.Services;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Service;
using MeuCrudCsharp.Features.Videos.Services;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Isso garante que até mesmo os erros de inicialização do host possam ser logados.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Define o nível mínimo de log a ser capturado (Debug, Info, Warning, Error, etc.)
    .MinimumLevel
    .Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Reduz o ruído dos logs internos do ASP.NET Core
    .Enrich.FromLogContext()
    .WriteTo.Console() // Continua escrevendo no console, como já faz hoje
    .WriteTo.File(
        "Log/log-.txt",
        rollingInterval: RollingInterval.Day,
        shared: true, // <-- A MUDANÇA MÁGICA ESTÁ AQUI
        flushToDiskInterval: TimeSpan.FromSeconds(1), // É bom adicionar isso quando 'shared' é true
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

try
{
    Log.Information("Iniciando a aplicação...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // --- Service Registration (Dependency Injection) ---

    // 1. Core ASP.NET Core Services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddSignalR();

    // 2. Main Database Configuration
    builder.Services.AddDbContextFactory<ApiDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); // Ajuste conforme seu BD


    // 3. ASP.NET Core Identity Configuration
    builder
        .Services.AddIdentity<Users, IdentityRole>(options => { options.SignIn.RequireConfirmedAccount = true; })
        .AddEntityFrameworkStores<ApiDbContext>()
        .AddDefaultTokenProviders();

    // Substitua o seu "builder.Services.ConfigureApplicationCookie" por este bloco completo:
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/ExternalLogin";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // --- LÓGICA ADICIONAL PARA APIs ---
        options.Events.OnRedirectToLogin = context =>
        {
            // Se a requisição for para um caminho de API...
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // ...não redirecione. Apenas retorne o status 401.
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            // Para qualquer outra requisição, mantenha o redirecionamento padrão.
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            // Faz o mesmo para quando o usuário está logado mas não tem a permissão (role)
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // Retorna 403 Forbidden
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

    // 4. In-Memory Cache
    // Registering this is useful as the custom CacheService implementation depends on it.
    builder.Services.AddMemoryCache();

    builder.Services.AddHangfireServer();

    // 5. Caching & Background Job Configuration (Conditional Redis vs. In-Memory)
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        // --- PRODUCTION ENVIRONMENT (with Redis) ---
        Console.WriteLine("--> Usando Redis para Cache Distribuído e Hangfire.");

        // 1. Register Redis implementation for the IDistributedCache abstraction.
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "MeuApp_"; // Optional prefix for keys in Redis
        });

        // 2. Configure Hangfire to use Redis.
        builder.Services.AddHangfire(config =>
            config
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseRedisStorage(redisConnectionString)
        );
    }
    else
    {
        // --- DEVELOPMENT ENVIRONMENT (without Redis) ---
        Console.WriteLine("--> Usando Cache em Memória para Cache Distribuído e Hangfire.");

        // 1. Register the IN-MEMORY implementation for the IDistributedCache abstraction.
        //    This allows the CacheService to work without a running Redis instance.
        builder.Services.AddDistributedMemoryCache();

        // 2. Configure Hangfire to use in-memory storage.
        builder.Services.AddHangfire(config =>
            config
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseInMemoryStorage()
        );
    }

    var mercadoPagoAccessToken = builder.Configuration["MercadoPago:AccessToken"];

    MercadoPagoConfig.AccessToken = mercadoPagoAccessToken;

    if (string.IsNullOrEmpty(MercadoPagoConfig.AccessToken))
    {
        Log.Fatal(
            "FALHA CRÍTICA: O Access Token do Mercado Pago (MercadoPago:AccessToken) não foi encontrado na configuração. A aplicação será encerrada.");

        throw new InvalidOperationException(
            "O Access Token do Mercado Pago é uma configuração obrigatória e não foi encontrada.");
    }
    else
    {
        Log.Information("Access Token do Mercado Pago carregado com sucesso.");
    }

    // 6. Custom Application Services
    builder.Services.AddScoped<ICacheService, CacheService>();
    builder.Services.AddScoped<IAppAuthService, AppAuthService>();
    builder.Services.AddScoped<ICreditCardPaymentService, CreditCardPaymentService>();
    builder.Services.AddScoped<IPreferencePayment, PreferencePaymentService>();
    builder.Services.AddScoped<IQueueService, BackgroundJobQueueService>();
    builder.Services.AddScoped<IEmailSenderService, SendGridEmailSenderService>();
    builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
    builder.Services.AddScoped<IAdminVideoService, AdminVideoService>();
    builder.Services.AddScoped<IAdminStudentService, AdminStudentService>();
    builder.Services.AddScoped<IUserAccountService, UserAccountService>();
    builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
    builder.Services.AddScoped<ICourseService, CourseService>();
    builder.Services.AddScoped<IRefundService, RefundService>();
    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
    builder.Services.AddScoped<IPlanService, PlanService>();
    builder.Services.AddScoped<ProcessPaymentNotificationJob>();
    builder.Services.AddScoped<IPaymentNotificationService, PaymentNotificationService>();
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<IRefundNotification, RefundNotification>();
    builder.Services.AddScoped<IMercadoPagoPaymentService, MercadoPagoPaymentService>();
    builder.Services.AddScoped<INotificationPayment, NotificationPayment>();
    builder.Services.AddScoped<IMercadoPagoPlanService, MercadoPagoPlanService>();
    builder.Services.AddSingleton<IAuthorizationHandler, ActiveSubscriptionHandler>();
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, SubscriptionAuthorizationMiddlewareResultHandler>();


    builder.Services.Configure<SendGridSettings>(
        builder.Configuration.GetSection(SendGridSettings.SectionName)
    );

    JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

    var cultureInfo = new CultureInfo("en-US"); // Ou CultureInfo.InvariantCulture
    CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
    CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

    // --- Authentication Configuration ---
    builder
        .Services.AddAuthentication()
        .AddGoogle(options =>
        {
            // Validação das credenciais (continua igual)
            string? clientId = builder.Configuration["Google:ClientId"];
            string? clientSecret = builder.Configuration["Google:ClientSecret"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException(
                    "As credenciais do Google não foram encontradas."
                );
            }

            options.ClientId = clientId;
            options.ClientSecret = clientSecret;

            // ✅ MUDANÇA CRÍTICA:
            // Diz ao Google para usar o esquema de cookie temporário do ASP.NET Identity.
            options.SignInScheme = IdentityConstants.ExternalScheme;
        })
        .AddJwtBearer(options =>
        {
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException(
                    "A chave JWT (Jwt:Key) não foi encontrada na configuração."
                );
            }

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateIssuer = false,
                ValidateAudience = false,

                NameClaimType = ClaimTypes.Name, // http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name
                RoleClaimType = ClaimTypes.Role, // http://schemas.microsoft.com/ws/2008/06/identity/claims/role
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Se o front-end não enviar o cabeçalho (o que acontecerá agora),
                    // o back-end procurará o token diretamente no cookie.
                    if (context.Request.Cookies.TryGetValue("jwt", out var tokenFromCookie))
                    {
                        context.Token = tokenFromCookie;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    // Program.cs
    builder.Services.AddAuthorization(options =>
    {
        // A política de JWT continua existindo se você precisar dela em outros lugares
        options.AddPolicy(
            "RequireJwtToken",
            policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
            });
        
        // NOVA POLÍTICA COMBINADA
        options.AddPolicy(
            "ActiveSubscription",
            policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ActiveSubscriptionRequirement());
            });
    });

    builder.Services.AddControllersWithViews().AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

    builder.Services.AddRazorPages(options =>
    {
        // 1. REGRA GERAL: Proteger TODAS as páginas por padrão.
        options.Conventions.AuthorizeFolder("/", "RequireJwtToken");

        // 2. EXCEÇÕES: Permitir acesso anônimo às páginas públicas.
        options.Conventions.AllowAnonymousToPage("/Index");

        // ✅ CORREÇÃO: Aponte para os novos caminhos, sem usar AllowAnonymousToAreaPage
        options.Conventions.AllowAnonymousToPage("/Account/ExternalLogin");
        options.Conventions.AllowAnonymousToPage("/Account/Logout");
    });

    builder.Services.Configure<RedirectSettings>(builder.Configuration.GetSection("Redirect"));
    builder.Services.Configure<PaymentSettings>(builder.Configuration.GetSection("Payment"));

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();

    builder.Services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.Lax; // Use Lax em vez de Strict para o fluxo de login externo
        options.OnAppendCookie = cookieContext =>
        {
            // Aplica SameSite=None apenas para os cookies de correlação e nonce do login externo
            if (
                cookieContext.CookieName.StartsWith(".AspNetCore.Correlation.")
                || cookieContext.CookieName.StartsWith(".AspNetCore.OpenIdConnect.Nonce.")
            )
            {
                cookieContext.CookieOptions.SameSite = SameSiteMode.None;
            }
        };
    });

    var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

// 1. ADICIONE A DEFINIÇÃO DA POLÍTICA DE CORS AQUI
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: myAllowSpecificOrigins,
            policy =>
            {
                policy.WithOrigins(
                        "https://074f5d6c3d9e.ngrok-free.app",
                        "http://localhost:5045"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });

    // 2. CONFIGURA A APLICAÇÃO PARA CONFIAR EM HEADERS DE PROXY (ESSENCIAL PARA NGROK)
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    // --- HTTP Request Pipeline Configuration ---
    var app = builder.Build();

    app.UseForwardedHeaders();

    app.UseCookiePolicy();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Seed initial roles (e.g., Admin, User) on application startup.
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "User", "Manager" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    // SignalR
    app.MapHub<VideoProcessingHub>("/videoProcessingHub");
    app.MapHub<RefundProcessingHub>("/RefundProcessingHub");
    app.MapHub<PaymentProcessingHub>("/PaymentProcessingHub");

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors(myAllowSpecificOrigins);
    app.UseRouting();

    // Order is important: Authentication must come before Authorization.
    app.UseAuthentication();
    app.UseAuthorization();

    // Add the Hangfire Dashboard (accessible at /hangfire).
    app.UseHangfireDashboard();

    app.MapRazorPages();
    app.MapControllers();
    app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação falhou ao iniciar.");
}
finally
{
    // ✅ 3. Garantir que os logs sejam salvos antes de a aplicação fechar
    Log.CloseAndFlush();
}