// 1. DEFINIÇÃO DOS TIPOS
// Define a "forma" dos dados que serão enviados para a API (Data Transfer Object).
interface ProdutoDTO {
    nome: string;
    preco: number;
    quantidade: number;
}

// Define a "forma" do objeto Produto que a API retorna, incluindo o ID.
// Herda as propriedades de ProdutoDTO.
interface Produto extends ProdutoDTO {
    id: number;
}


// 2. SELEÇÃO SEGURA DOS ELEMENTOS DO DOM
// Selecionamos os elementos e informamos ao TypeScript qual é o tipo de cada um.
// O uso de 'as' é uma "afirmação de tipo" (type assertion).
const form = document.getElementById('formCadastro') as HTMLFormElement;
const nomeInput = document.getElementById('nome') as HTMLInputElement;
const precoInput = document.getElementById('preco') as HTMLInputElement;
const quantidadeInput = document.getElementById('quantidade') as HTMLInputElement;
const resultadoDiv = document.getElementById('resultado') as HTMLDivElement;


// 3. LÓGICA DO EVENTO COM TIPAGEM
// Aguarda o formulário ser enviado
form.addEventListener('submit', async (event: SubmitEvent) => {
    // O parâmetro 'event' agora é do tipo 'SubmitEvent'.
    event.preventDefault();

    // Pega os valores dos campos do formulário. O TypeScript já sabe que '.value' existe.
    const nome = nomeInput.value;
    // A função parseFloat já retorna um 'number'.
    const preco = parseFloat(precoInput.value);
    // É uma boa prática especificar a base (radix) 10 para o parseInt.
    const quantidade = parseInt(quantidadeInput.value, 10);

    // Validação para garantir que os números são válidos
    if (isNaN(preco) || isNaN(quantidade)) {
        resultadoDiv.innerHTML = `<p><strong>Erro:</strong> Preço e Quantidade devem ser números válidos.</p>`;
        return;
    }

    // Monta o objeto com os dados, garantindo que ele siga a interface 'ProdutoDTO'.
    const produtoParaEnviar: ProdutoDTO = {
        nome: nome,
        preco: preco,
        quantidade: quantidade
    };

    // Envia os dados para a API C#
    try {
        const response = await fetch('/api/produto', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(produtoParaEnviar)
        });

        // Verifica a resposta do servidor
        if (response.ok) {
            // O TypeScript sabe que 'produtoCriado' deve ter o formato da interface 'Produto'.
            const produtoCriado: Produto = await response.json();
            resultadoDiv.innerHTML = `
        <p><strong>Produto criado com sucesso!</strong></p>
        <p>ID: ${produtoCriado.id}</p>
        <p>Nome: ${produtoCriado.nome}</p>
      `;
        } else {
            const erro = await response.text();
            resultadoDiv.innerHTML = `
        <p><strong>Erro ao criar produto:</strong></p>
        <p>${response.status} - ${erro}</p>
      `;
        }
    } catch (error) {
        // Tratamento para erros de rede.
        console.error('Erro de rede:', error);
        if (resultadoDiv) {
            resultadoDiv.innerText = 'Falha na comunicação com o servidor.';
        }
    }
});