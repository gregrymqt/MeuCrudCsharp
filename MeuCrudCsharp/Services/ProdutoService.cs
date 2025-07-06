using MeuCrudCsharp.Data;
using MeuCrudCsharp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Services
{
    public class ProdutoService
    {
        private readonly ApiDbContext _context;

        public ProdutoService(ApiDbContext context)
        {
            _context = context;
        }

        // 1. O método agora retorna o próprio objeto 'Produto' que foi salvo.
        // É responsabilidade do Controller transformar isso em uma resposta HTTP.
        public async Task<Produto> SalvarProdutoAsync(Produto produto)
        {
            _context.Produtos.Add(produto);
            await _context.SaveChangesAsync();

            // Após salvar, o EF Core atualiza o objeto 'produto' com o Id do banco.
            // Retornamos o objeto completo.
            return produto;
        }

        // 2. O método de busca também retorna o objeto 'Produto' ou 'null'.
        // O [HttpGet] e o ActionResult<T> foram removidos, pois pertencem ao Controller.
        // O '?' em 'Produto?' indica que o resultado pode ser nulo (se não encontrar).
        public async Task<Produto?> GetProdutoAsync(int id)
        {
            // FindAsync já retorna nulo se não encontrar, então o código fica mais simples.
            return await _context.Produtos.FindAsync(id);
        }
    }
}