// /js/modules/api/paymentAPI.js

// Importa a função que busca o token. Ajuste o caminho se for diferente.
import { getAuthToken } from '../../../../token/getTokens.js';

/**
 * NOVO: Função central e automática para todas as chamadas de API deste módulo.
 * Pega o token automaticamente e lida com toda a lógica de requisição e resposta.
 * @param {string} url - O endpoint da API.
 * @param {object} options - Opções do fetch (method, body, etc.).
 * @returns {Promise<any>} - A resposta da API em formato JSON.
 */
async function apiFetch(url, options = {}) {
    // 1. Pega o token automaticamente.
    const token = getAuthToken();

    // 2. Configura os cabeçalhos.
    const headers = { ...options.headers };
    
    // 3. Adiciona o token ao cabeçalho SOMENTE se ele existir.
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    // Adiciona Content-Type automaticamente se houver corpo na requisição
    if (options.body && (options.method === 'POST' || options.method === 'PUT')) {
        headers['Content-Type'] = 'application/json';
    }

    // 4. Realiza a chamada fetch
    const response = await fetch(url, { ...options, headers });

    // 5. Lida com a resposta
    if (response.status === 204) { // Sucesso sem conteúdo
        return null;
    }

    const contentType = response.headers.get("content-type");
    if (!contentType || !contentType.includes("application/json")) {
        const text = await response.text();
        console.error("Resposta inesperada do servidor:", text);
        throw new Error(`O servidor respondeu com um formato inesperado (não-JSON). Status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!response.ok) {
        const errorMessage = data.message || `Erro na API. Status: ${response.status}`;
        throw new Error(errorMessage);
    }

    return data;
}

/**
 * REATORADO: Envia os dados do formulário de pagamento para o back-end.
 * Agora utiliza a função central apiFetch para maior robustez e consistência.
 * @param {object} formData - Os dados do formulário coletados pelo Brick.
 * @returns {Promise<object>} A resposta da API.
 */
export function processPayment(formData) {
    // A chamada agora é uma linha simples que usa nossa função central.
    // O token é adicionado automaticamente e a resposta é validada.
    return apiFetch(window.paymentConfig.processPaymentUrl, {
        method: "POST",
        body: JSON.stringify(formData)
    });
}