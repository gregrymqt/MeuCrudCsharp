// Aguarda o formulário ser enviado
document.getElementById('formCadastro').addEventListener('submit', async function (event) {
    // Impede o comportamento padrão do formulário (que é recarregar a página)
    event.preventDefault();

    // 1. Pega os valores dos campos do formulário
    const nome = document.getElementById('nome').value;
    const preco = parseFloat(document.getElementById('preco').value);
    const quantidade = parseInt(document.getElementById('quantidade').value);

    // 2. Monta um objeto JavaScript com os dados
    const produto = {
        nome: nome,
        preco: preco,
        quantidade: quantidade
    };

    // 3. FAZ A MÁGICA: Envia os dados para a API C#
    try {
        const response = await fetch('/api/produto', {
            method: 'POST', // O tipo de requisição
            headers: {
                'Content-Type': 'application/json' // Dizendo que estamos enviando JSON
            },
            body: JSON.stringify(produto) // Converte o objeto JS para uma string JSON
        });

        const resultadoDiv = document.getElementById('resultado');

        // 4. Verifica a resposta do servidor
        if (response.ok) { // Se a resposta for 2xx (ex: 201 Created)
            const produtoCriado = await response.json(); // Pega o produto criado retornado pela API
            resultadoDiv.innerHTML = `
                <p><strong>Produto criado com sucesso!</strong></p>
                <p>ID: ${produtoCriado.id}</p>
                <p>Nome: ${produtoCriado.nome}</p>
            `;
        } else {
            // Se o servidor retornou um erro (ex: 400 Bad Request)
            const erro = await response.text();
            resultadoDiv.innerHTML = `
                <p><strong>Erro ao criar produto:</strong></p>
                <p>${response.status} - ${erro}</p>
            `;
        }
    } catch (error) {
        // Se houver um erro de rede
        console.error('Erro de rede:', error);
        document.getElementById('resultado').innerText = 'Falha na comunicação com o servidor.';
    }
});