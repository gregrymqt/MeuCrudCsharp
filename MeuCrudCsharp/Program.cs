using MeuCrudCsharp.Data;
using MeuCrudCsharp.Services; // Adicione o using para a pasta de serviços
using MeuCrudCsharp.Models; // Garante que o Program.cs enxergue a classe ApplicationUserusing
using Microsoft.AspNetCore.Authentication.Cookies; // Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;


var builder = WebApplication.CreateBuilder(args);

// --- Registro de Serviços ---

// Registra os serviços necessários para que seus Controllers funcionem.
builder.Services.AddControllers();

// 1. Configuração do DbContext
// Equivale a configurar o 'database.php' no Laravel.
// Você está dizendo: "Para qualquer um que precise do banco de dados (ApiDbContext),
// use o SQL Server com a string de conexão 'DefaultConnection' que está no appsettings.json".
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. REGISTRO DO SEU SERVIÇO
// Equivalente a um app()->bind() ou app()->scoped() no Laravel.
// Você está registrando sua classe de serviço 'ProdutoService' no contêiner de DI.
// 'AddScoped' significa que uma nova instância será criada para cada requisição HTTP.
builder.Services.AddScoped<ProdutoService>();

//servico de autenticação para login
builder.Services.AddScoped<IAppAuthService, AppAuthService>();

builder.Services.AddSingleton<TokenMercadoPago>();

// Adicione esta linha! Ela registra todos os serviços necessários para Razor Pages.
builder.Services.AddRazorPages();

// Registra os serviços para gerar a documentação da API (Swagger).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Adiciona os serviços de autenticação
// Aqui você registra os "drivers" de autenticação.
builder.Services.AddAuthentication(options =>
{
    // Define que o esquema padrão para login é via Cookie.
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Se um usuário não autenticado tentar acessar uma área restrita,
    // ele será desafiado pelo esquema do Google (redirecionado para a tela de login do Google).
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie() // Adiciona o handler para gerenciar a sessão do usuário com um cookie após o login.
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    // 1. Lê as configurações em variáveis que podem ser nulas.
    string? clientId = builder.Configuration["Google:ClientId"];
    string? clientSecret = builder.Configuration["Google:ClientSecret"];

    // 2. Verifica se as configurações foram realmente encontradas.
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        // 3. Se não foram, lança uma exceção clara. A aplicação não pode funcionar sem elas.
        // É melhor falhar agora do que durante uma tentativa de login.
        throw new InvalidOperationException("As credenciais do Google (ClientId e ClientSecret) não foram encontradas na configuração. Verifique o User Secrets ou appsettings.json.");
    }

    // 4. Se tudo estiver ok, atribui os valores. O compilador agora sabe que eles não são nulos.
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;

    // No Program.cs, dentro do .AddGoogle(...)
    options.Events.OnCreatingTicket = context =>
    {
        // 1. EXTRAIR AS INFORMAÇÕES (CLAIMS) DO GOOGLE
        var principal = context.Principal;
        if (principal == null)
        {
            return Task.CompletedTask;
        }

        // Pega o ID único do Google
        string googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string email = principal.FindFirstValue(ClaimTypes.Email);
        string name = principal.FindFirstValue(ClaimTypes.Name);
        string avatar = principal.FindFirstValue("urn:google:picture"); // Claim específica para a foto de perfil


        // VERIFICAÇÃO DE SEGURANÇA: Se não conseguimos o ID ou o Email, algo deu errado.
        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            // Apenas para o processo sem criar um usuário.
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
                    // Usamos o operador '??' para fornecer um valor padrão caso seja nulo.
                    GoogleId = googleId,
                    Email = email,
                    Name = name ?? "Usuário Anônimo",
                    AvatarUrl = avatar
                };
                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();
                // O usuário a ser autenticado agora é o recém-criado
                user = newUser;
            }

            // 4. REALIZAR A AUTENTICAÇÃO LOCAL (A PARTE DO "identify.authentic")
            // Resolve o seu serviço de autenticação
            var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();

            // Passa o usuário (novo ou existente) e o HttpContext para criar a sessão/cookie
            // Usamos .Result aqui porque o evento não é async por padrão
            authService.SignInUser(user, context.HttpContext).Wait();
        }

        return Task.CompletedTask;
    };
});

// Registra serviços para que você possa usar Controllers e Views juntos (padrão MVC).
builder.Services.AddControllersWithViews();

// --- Construção e Configuração do Pipeline HTTP ---
var app = builder.Build(); // Constrói a aplicação com todos os serviços registrados acima.

// Apenas em ambiente de desenvolvimento, habilita o middleware do Swagger.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Permite acessar a interface gráfica do Swagger no navegador.
}

// Habilita o middleware de roteamento para encontrar qual endpoint corresponde à URL.
app.UseRouting();

// Habilita o middleware para servir arquivos estáticos (JS, CSS, imagens) da pasta wwwroot.
app.UseStaticFiles();

// 3. Habilita o middleware de autenticação e autorização.
// A ordem correta e mais comum é: Roteamento -> Autenticação -> Autorização.
app.UseAuthentication(); // Verifica QUEM é o usuário (lê o cookie, etc.).
app.UseAuthorization();  // Verifica O QUE o usuário autenticado PODE FAZER.

// Tenta servir um arquivo padrão (como index.html) para a rota raiz.
app.UseDefaultFiles();

// Redireciona requisições HTTP para HTTPS.
app.UseHttpsRedirection();

//para os razorPages
app.MapRazorPages();

// Mapeia as rotas para os seus Controllers.
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicia a aplicação e começa a escutar por requisições HTTP.
app.Run();