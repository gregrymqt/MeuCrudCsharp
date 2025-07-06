using MeuCrudCsharp.Data;
using MeuCrudCsharp.Services; // Adicione o using para a pasta de serviços
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Registro de Serviços ---

builder.Services.AddControllers();

// 1. Configuração do DbContext
builder.Services.AddDbContext<ApiDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. REGISTRO DO SEU SERVIÇO (A LINHA QUE FALTAVA)
builder.Services.AddScoped<ProdutoService>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Construção e Configuração do Pipeline HTTP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configuração do Frontend
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();