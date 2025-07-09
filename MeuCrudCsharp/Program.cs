using MeuCrudCsharp.Data;
using MeuCrudCsharp.Services; // Adicione o using para a pasta de servi�os
using MeuCrudCsharp.Models; // Garante que o Program.cs enxergue a classe ApplicationUserusing
using Microsoft.AspNetCore.Authentication.Cookies; // Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;


var builder = WebApplication.CreateBuilder(args);

// --- Registro de Servi�os ---

// Registra os servi�os necess�rios para que seus Controllers funcionem.
builder.Services.AddControllers();

// 1. Configura��o do DbContext
// Equivale a configurar o 'database.php' no Laravel.
// Voc� est� dizendo: "Para qualquer um que precise do banco de dados (ApiDbContext),
// use o SQL Server com a string de conex�o 'DefaultConnection' que est� no appsettings.json".
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. REGISTRO DO SEU SERVI�O
// Equivalente a um app()->bind() ou app()->scoped() no Laravel.
// Voc� est� registrando sua classe de servi�o 'ProdutoService' no cont�iner de DI.
// 'AddScoped' significa que uma nova inst�ncia ser� criada para cada requisi��o HTTP.
builder.Services.AddScoped<ProdutoService>();

//servico de autentica��o para login
builder.Services.AddScoped<IAppAuthService, AppAuthService>();

builder.Services.AddSingleton<TokenMercadoPago>();

// Adicione esta linha! Ela registra todos os servi�os necess�rios para Razor Pages.
builder.Services.AddRazorPages();

// Registra os servi�os para gerar a documenta��o da API (Swagger).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Adiciona os servi�os de autentica��o
// Aqui voc� registra os "drivers" de autentica��o.
builder.Services.AddAuthentication(options =>
{
    // Define que o esquema padr�o para login � via Cookie.
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    // Se um usu�rio n�o autenticado tentar acessar uma �rea restrita,
    // ele ser� desafiado pelo esquema do Google (redirecionado para a tela de login do Google).
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie() // Adiciona o handler para gerenciar a sess�o do usu�rio com um cookie ap�s o login.
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    // 1. L� as configura��es em vari�veis que podem ser nulas.
    string? clientId = builder.Configuration["Google:ClientId"];
    string? clientSecret = builder.Configuration["Google:ClientSecret"];

    // 2. Verifica se as configura��es foram realmente encontradas.
    if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
    {
        // 3. Se n�o foram, lan�a uma exce��o clara. A aplica��o n�o pode funcionar sem elas.
        // � melhor falhar agora do que durante uma tentativa de login.
        throw new InvalidOperationException("As credenciais do Google (ClientId e ClientSecret) n�o foram encontradas na configura��o. Verifique o User Secrets ou appsettings.json.");
    }

    // 4. Se tudo estiver ok, atribui os valores. O compilador agora sabe que eles n�o s�o nulos.
    options.ClientId = clientId;
    options.ClientSecret = clientSecret;

    // No Program.cs, dentro do .AddGoogle(...)
    options.Events.OnCreatingTicket = context =>
    {
        // 1. EXTRAIR AS INFORMA��ES (CLAIMS) DO GOOGLE
        var principal = context.Principal;
        if (principal == null)
        {
            return Task.CompletedTask;
        }

        // Pega o ID �nico do Google
        string googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        string email = principal.FindFirstValue(ClaimTypes.Email);
        string name = principal.FindFirstValue(ClaimTypes.Name);
        string avatar = principal.FindFirstValue("urn:google:picture"); // Claim espec�fica para a foto de perfil


        // VERIFICA��O DE SEGURAN�A: Se n�o conseguimos o ID ou o Email, algo deu errado.
        if (string.IsNullOrEmpty(googleId) || string.IsNullOrEmpty(email))
        {
            // Apenas para o processo sem criar um usu�rio.
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
                    // Usamos o operador '??' para fornecer um valor padr�o caso seja nulo.
                    GoogleId = googleId,
                    Email = email,
                    Name = name ?? "Usu�rio An�nimo",
                    AvatarUrl = avatar
                };
                dbContext.Users.Add(newUser);
                dbContext.SaveChanges();
                // O usu�rio a ser autenticado agora � o rec�m-criado
                user = newUser;
            }

            // 4. REALIZAR A AUTENTICA��O LOCAL (A PARTE DO "identify.authentic")
            // Resolve o seu servi�o de autentica��o
            var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();

            // Passa o usu�rio (novo ou existente) e o HttpContext para criar a sess�o/cookie
            // Usamos .Result aqui porque o evento n�o � async por padr�o
            authService.SignInUser(user, context.HttpContext).Wait();
        }

        return Task.CompletedTask;
    };
});

// Registra servi�os para que voc� possa usar Controllers e Views juntos (padr�o MVC).
builder.Services.AddControllersWithViews();

// --- Constru��o e Configura��o do Pipeline HTTP ---
var app = builder.Build(); // Constr�i a aplica��o com todos os servi�os registrados acima.

// Apenas em ambiente de desenvolvimento, habilita o middleware do Swagger.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Permite acessar a interface gr�fica do Swagger no navegador.
}

// Habilita o middleware de roteamento para encontrar qual endpoint corresponde � URL.
app.UseRouting();

// Habilita o middleware para servir arquivos est�ticos (JS, CSS, imagens) da pasta wwwroot.
app.UseStaticFiles();

// 3. Habilita o middleware de autentica��o e autoriza��o.
// A ordem correta e mais comum �: Roteamento -> Autentica��o -> Autoriza��o.
app.UseAuthentication(); // Verifica QUEM � o usu�rio (l� o cookie, etc.).
app.UseAuthorization();  // Verifica O QUE o usu�rio autenticado PODE FAZER.

// Tenta servir um arquivo padr�o (como index.html) para a rota raiz.
app.UseDefaultFiles();

// Redireciona requisi��es HTTP para HTTPS.
app.UseHttpsRedirection();

//para os razorPages
app.MapRazorPages();

// Mapeia as rotas para os seus Controllers.
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Inicia a aplica��o e come�a a escutar por requisi��es HTTP.
app.Run();