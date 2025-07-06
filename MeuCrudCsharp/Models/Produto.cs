using System.ComponentModel.DataAnnotations.Schema;

namespace MeuCrudCsharp.Models
{
    public class Produto
    {
        public int Id { get; set; }
        public string Nome { get; set; }

        [Column(TypeName = "decimal(18, 2)")] // <-- ADICIONE ESTA ANOTAÇÃO
        public decimal Preco { get; set; }

        public int Quantidade { get; set; }
    }
}
