using System.Security.Claims;
using System.Text;
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
using MeuCrudCsharp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

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

// Adds authorization services to the container.
builder.Services.AddAuthorization();

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
builder.Services.AddTransient<VideoProcessingService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<ProcessPaymentNotificationJob>();
builder.Services.AddScoped<INotificationPaymentService, NotificationPaymentService>();
builder.Services.AddScoped<IQueueService, BackgroundJobQueueService>();

// 7. Hangfire Server
// This adds the background processing server for Hangfire jobs.
// It must come after Hangfire has been configured (AddHangfire).
builder.Services.AddHangfireServer();

// --- Authentication Configuration ---
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            ),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
        options.Events = new JwtBearerEvents
        {
            // This event allows the app to read the JWT from a cookie, which is useful for browser-based clients.
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
    .AddGoogle(
        GoogleDefaults.AuthenticationScheme,
        options =>
        {
            string? clientId = builder.Configuration["Google:ClientId"];
            string? clientSecret = builder.Configuration["Google:ClientSecret"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException(
                    "Google credentials (ClientId and ClientSecret) were not found in the configuration."
                );
            }

            options.ClientId = clientId;
            options.ClientSecret = clientSecret;

            // Event to synchronize the Google user with the local database upon successful login.
            options.Events.OnCreatingTicket = context =>
            {
                var principal = context.Principal;
                if (principal == null)
                {
                    return Task.CompletedTask; // Should not happen
                }

                // Declare variables as 'string?' to handle potential nulls from FindFirstValue.
                string? googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                string? email = principal.FindFirstValue(ClaimTypes.Email);
                string? name = principal.FindFirstValue(ClaimTypes.Name);
                string? avatar = principal.FindFirstValue("urn:google:picture");

                if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
                {
                    return Task.CompletedTask; // Not a valid Google user for this app.
                }

                // Create a new DI scope to resolve services, as this event handler is a singleton.
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
                            Name = name ?? "Anonymous User",
                            AvatarUrl = avatar ?? string.Empty, // Add a default value to avoid nulls
                        };
                        dbContext.Users.Add(newUser);
                        dbContext.SaveChanges();
                        user = newUser;
                    }

                    var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();
                    // Use .Wait() because this event handler is not naturally async.
                    authService.SignInUser(user, context.HttpContext).Wait();
                }

                return Task.CompletedTask;
            };
        }
    );

builder.Services.AddControllersWithViews();

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
