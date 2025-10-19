using DotNetEnv;
using MeuCrudCsharp.Extensions;
using Serilog;

// Isso garante que até mesmo os erros de inicialização do host possam ser logados.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug() // Define o nível mínimo de log a ser capturado (Debug, Info, Warning, Error, etc.)
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning) // Reduz o ruído dos logs internos do ASP.NET Core
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
    Env.Load();

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // --- 2. Registro de Serviços (usando os Métodos de Extensão) ---
    builder
        .AddCoreServices() // Configura Controllers, Razor Pages, Swagger, SignalR
        .AddApplicationServices() // Registra todos os seus serviços de negócio
        .AddPersistence() // Configura DB, Identity, Cache (Redis) e Hangfire
        .AddWebServices() // Configura CORS, Cookies e HttpClient
        .AddAuth(); // Configura Autenticação e Autorização

    // --- 3. Construção e Configuração do Pipeline ---
    var app = builder.Build();

    await app.UseAppPipeline(); // Configura todos os middlewares e endpoints

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "A aplicação falhou ao iniciar: {ExceptionMessage}", ex.Message);
    Console.WriteLine($"FATAL EXCEPTION: {ex}"); // Added for immediate console visibility
    Console.WriteLine($"STACK TRACE: {ex.StackTrace}"); // Added for immediate console visibility
}
finally
{
    Log.CloseAndFlush();
}
