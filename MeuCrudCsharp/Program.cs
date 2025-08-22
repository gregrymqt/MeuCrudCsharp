using Hangfire;
using Hangfire.Redis.StackExchange;
using MercadoPago.Config;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth;
using MeuCrudCsharp.Features.Clients.Interfaces;
using MeuCrudCsharp.Features.Clients.Service;
using MeuCrudCsharp.Features.Courses.Interfaces;
using MeuCrudCsharp.Features.Courses.Services;
using MeuCrudCsharp.Features.Emails.Interfaces;
using MeuCrudCsharp.Features.Emails.Services;
using MeuCrudCsharp.Features.Hubs;
using MeuCrudCsharp.Features.MercadoPago.Jobs; // Adicionado para os Jobs
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Services;
using MeuCrudCsharp.Features.Plans.Interfaces;
using MeuCrudCsharp.Features.Plans.Services;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Profiles.Admin.Services;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.Services;
using MeuCrudCsharp.Features.Refunds.Interfaces;
using MeuCrudCsharp.Features.Refunds.Services;
using MeuCrudCsharp.Features.Subscriptions.Interfaces;
using MeuCrudCsharp.Features.Subscriptions.Services;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Service;
using MeuCrudCsharp.Features.Videos.Services;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration (Dependency Injection) ---

// 1. Core ASP.NET Core Services
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Main Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(connectionString));

// 3. ASP.NET Core Identity Configuration
builder
    .Services.AddDefaultIdentity<Users>(options =>
    {
        // Optional password settings, etc.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>() // Adds support for Roles
    .AddEntityFrameworkStores<ApiDbContext>();

// 4. In-Memory Cache
// Registering this is useful as the custom CacheService implementation depends on it.
builder.Services.AddMemoryCache();

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

MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];

// 6. Custom Application Services
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IAppAuthService, AppAuthService>();
builder.Services.AddScoped<ICreditCardPayments, CreditCardPaymentService>();
builder.Services.AddScoped<IPreferencePayment, PreferencePaymentService>();
builder.Services.AddScoped<IQueueService, BackgroundJobQueueService>();
builder.Services.AddScoped<IEmailSenderService, SendGridEmailSenderService>();
builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
builder.Services.AddScoped<IAdminVideoService, AdminVideoService>();
builder.Services.AddScoped<IAdminStudentService, AdminStudentService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<ProcessPaymentNotificationJob>();
builder.Services.AddScoped<INotificationPaymentService, NotificationPaymentService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();



// 7. Hangfire Server
// This adds the background processing server for Hangfire jobs.
// It must come after Hangfire has been configured (AddHangfire).
builder.Services.AddHangfireServer();

builder.Services.Configure<SendGridSettings>(
    builder.Configuration.GetSection(SendGridSettings.SectionName)
);

// --- Authentication Configuration ---
builder
    .Services.AddAuthentication(options =>
    {
        // O esquema padrão para autenticar e desafiar o usuário será o JWT.
        // A aplicação vai primariamente procurar por um JWT válido.
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        // O handler de cookie é usado internamente pelo Google para o processo de redirecionamento (OAuth).
        // Não será o método principal de autenticação da sessão do usuário.
        options.Cookie.Name = "ExternalLoginCookie";
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), // Usa a variável validada
            ValidateIssuer = false,
            ValidateAudience = false,
        };
        // Evento para ler o token JWT do cookie que vamos criar.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("jwt", out var token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
        };
    })
    // DENTRO DO SEU PROGRAM.CS

    .AddGoogle(options =>
    {
        // Validação das credenciais (continua igual)
        string? clientId = builder.Configuration["Google:ClientId"];
        string? clientSecret = builder.Configuration["Google:ClientSecret"];
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("As credenciais do Google não foram encontradas.");
        }
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;

        // ✅ MUDANÇA CRÍTICA:
        // Diz ao Google para usar o esquema de cookie temporário do ASP.NET Identity.
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "RequireJwtToken",
        policy =>
        {
            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
        }
    );
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter("RequireJwtToken")
    );
});

// --- HTTP Request Pipeline Configuration ---
var app = builder.Build();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
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
