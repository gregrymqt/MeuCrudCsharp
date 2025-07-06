using Microsoft.AspNetCore.Mvc;
using MeuCrudCsharp.Models;
using MeuCrudCsharp.Services;
using System.Threading.Tasks;

namespace MeuCrudCsharp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProdutoController : ControllerBase
    {
        private readonly ProdutoService _produtoService;

        public ProdutoController(ProdutoService produtoService)
        {
            _produtoService = produtoService;
        }

        [HttpPost]
        public async Task<IActionResult> CriarProduto([FromBody] Produto produto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Chama o serviço para salvar e AGUARDA (await) o resultado.
            // A variável 'produtoCriado' vai conter o produto com o ID preenchido.
            var produtoCriado = await _produtoService.SalvarProdutoAsync(produto);

            // 2. Monta a resposta HTTP 201 Created.
            //    - Usa o nome do método GET deste próprio controller.
            //    - Usa o ID do objeto que o *serviço retornou*.
            //    - Retorna o objeto completo no corpo da resposta.
            return CreatedAtAction(nameof(GetProduto), new { id = produtoCriado.Id }, produtoCriado);
        }

        // 3. Este é o endpoint GET público que o CreatedAtAction usa para montar a URL.
        //    Ele também usa o serviço para buscar os dados.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduto(int id)
        {
            var produto = await _produtoService.GetProdutoAsync(id);

            // É o controller que decide: se o serviço retornou nulo, a resposta é 404 Not Found.
            if (produto == null)
            {
                return NotFound();
            }

            // Se encontrou, a resposta é 200 OK com o produto.
            return Ok(produto);
        }
    }
}