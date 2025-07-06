using MeuCrudCsharp.Data;
using MeuCrudCsharp.Services; // Adicione o using para a pasta de servi�os
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Registro de Servi�os ---

builder.Services.AddControllers();

// 1. Configura��o do DbContext
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. REGISTRO DO SEU SERVI�O (A LINHA QUE FALTAVA)
builder.Services.AddScoped<ProdutoService>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Constru��o e Configura��o do Pipeline HTTP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configura��o do Frontend
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();