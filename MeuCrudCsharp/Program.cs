using Hangfire;
using Hangfire.Redis.StackExchange;
using MeuCrudCsharp.Data;
using MeuCrudCsharp.Features.Auth;
using MeuCrudCsharp.Features.MercadoPago.Jobs; // Adicionado para os Jobs
using MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces;
using MeuCrudCsharp.Features.MercadoPago.Payments.Services;
using MeuCrudCsharp.Features.MercadoPago.Tokens;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using MeuCrudCsharp.Features.Profiles.Admin.Services;
using MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces;
using MeuCrudCsharp.Features.Profiles.UserAccount.Services;
using MeuCrudCsharp.Features.Videos.Interfaces;
using MeuCrudCsharp.Features.Videos.Service;
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// --- Registro de Serviços (Injeção de Dependência) ---

// 1. Registros essenciais do ASP.NET Core
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Configuração do Banco de Dados Principal
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiDbContext>(options => options.UseSqlServer(connectionString));

// 2. Adicionar o ASP.NET Core Identity
builder
    .Services.AddDefaultIdentity<Users>(options =>
    {
        // Configurações opcionais de senha, etc.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>() // <-- Adiciona o suporte a Roles
    .AddEntityFrameworkStores<ApiDbContext>();

builder.Services.AddAuthentication(); // Adiciona os serviços de autenticação
builder.Services.AddAuthorization();

// 3. Habilita o serviço de cache em memória nativo do .NET
// É bom registrar isso aqui, pois a implementação MemoryCacheService depende dele.
builder.Services.AddMemoryCache();

// 4. Configuração do Cache e da Fila (Decide entre Redis e Memória)
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrEmpty(redisConnectionString))
{
    // --- Configuração para ambiente com REDIS ---
    Console.WriteLine("--> Usando Redis como serviço de cache e para a fila do Hangfire.");

    // Registra a conexão com o Redis como um Singleton
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString)
    );

    // Registra a implementação de cache com Redis
    builder.Services.AddScoped<ICacheService, RedisCacheService>();

    // Configura o Hangfire para usar o Redis (MOVEMOS PARA DENTRO DO IF)
    builder.Services.AddHangfire(config =>
        config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseRedisStorage(redisConnectionString)
    );
}
else
{
    // --- Configuração para ambiente SEM REDIS ---
    Console.WriteLine(
        "--> Usando Cache em Memória. AVISO: Hangfire usará armazenamento em memória, não recomendado para produção."
    );

    // Registra a implementação de cache em memória
    builder.Services.AddScoped<ICacheService, MemoryCacheService>();

    // Configura o Hangfire para usar armazenamento em memória como um fallback
    // (É preciso instalar o pacote Hangfire.InMemory)
    builder.Services.AddHangfire(config =>
        config
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseInMemoryStorage()
    );
}

// Adiciona o HttpClient para ser injetado no serviço do Mercado Pago
builder.Services.AddHttpClient<IMercadoPagoService, MercadoPagoService>(client =>
{
    client.BaseAddress = new Uri("https://api.mercadopago.com");
});

// 5. Registrando seus serviços customizados da aplicação
builder.Services.AddScoped<IMercadoPagoService, MercadoPagoService>();
builder.Services.AddScoped<IAppAuthService, AppAuthService>();
builder.Services.AddScoped<ICreditCardPayments, CreditCardPaymentService>();
builder.Services.AddScoped<IPreferencePayment, PreferencePaymentService>();
builder.Services.AddScoped<IQueueService, BackgroundJobQueueService>();
builder.Services.AddScoped<IEmailSenderService, SendGridEmailSenderService>();
builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
builder.Services.AddScoped<IAdminVideoService, AdminVideoService>();
builder.Services.AddScoped<IAdminStudentService, AdminStudentService>();
builder.Services.AddHttpClient<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddSingleton<TokenMercadoPago>();
builder.Services.AddTransient<VideoProcessingService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddHttpContextAccessor();

// 6. Adiciona o servidor Hangfire que processa os jobs na fila
// Isso deve vir depois que o Hangfire foi configurado (AddHangfire)
builder.Services.AddHangfireServer();

// --- Configuração da Autenticação ---
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(
        GoogleDefaults.AuthenticationScheme,
        options =>
        {
            string? clientId = builder.Configuration["Google:ClientId"];
            string? clientSecret = builder.Configuration["Google:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException(
                    "As credenciais do Google (ClientId e ClientSecret) não foram encontradas na configuração."
                );
            }

            options.ClientId = clientId;
            options.ClientSecret = clientSecret;

            // Evento para sincronizar o usuário do Google com o banco de dados local
            options.Events.OnCreatingTicket = context =>
            {
                var principal = context.Principal;
                if (principal == null)
                {
                    return Task.CompletedTask;
                }

                // CORREÇÃO: Declaramos as variáveis como 'string?' para aceitar nulos de FindFirstValue
                string? googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                string? email = principal.FindFirstValue(ClaimTypes.Email);
                string? name = principal.FindFirstValue(ClaimTypes.Name);
                string? avatar = principal.FindFirstValue("urn:google:picture");

                if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
                {
                    return Task.CompletedTask;
                }

                using (var scope = context.HttpContext.RequestServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
                    var user = dbContext.Users.FirstOrDefault(u => u.GoogleId == googleId);

                    if (user == null)
                    {
                        var newUser = new Users
                        {
                            GoogleId = googleId,
                            Email = email,
                            Name = name ?? "Usuário Anônimo",
                            // CORREÇÃO: Adicionamos um valor padrão para evitar nulos
                            AvatarUrl = avatar ?? string.Empty,
                        };
                        dbContext.Users.Add(newUser);
                        dbContext.SaveChanges();
                        user = newUser;
                    }

                    var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();
                    // Usamos .Wait() porque este evento não é naturalmente async
                    authService.SignInUser(user, context.HttpContext).Wait();
                }

                return Task.CompletedTask;
            };
        }
    );

builder.Services.AddControllersWithViews();

// --- Construção e Configuração do Pipeline HTTP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// A ordem é importante: Autenticação primeiro, depois Autorização.
app.UseAuthentication();
app.UseAuthorization();

// Adiciona o Dashboard do Hangfire (acessível em /hangfire)
app.UseHangfireDashboard();

app.MapRazorPages();
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
