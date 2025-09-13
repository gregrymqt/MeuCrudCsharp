// /js/core/apiService.js

// Variáveis de escopo do módulo para guardar os tokens
let antiforgeryToken = null;
let antiforgeryHeaderName = null;

/**
 * Busca o token antiforgery do servidor se ainda não o tivermos.
 * Esta função é interna do módulo e não precisa ser exportada.
 */
async function ensureAntiforgeryToken() {
    if (!antiforgeryToken) {
        try {
            const response = await fetch('/api/antiforgery/token', { credentials: 'include' });
            if (response.ok) {
                const data = await response.json();
                antiforgeryToken = data.token;
                antiforgeryHeaderName = data.headerName;
                console.log("Token Antiforgery obtido e armazenado.");
            }
        } catch (error) {
            console.error("Falha ao obter o token Antiforgery:", error);
        }
    }
}

/**
 * Função de fetch unificada e inteligente para todas as chamadas de API.
 * Lida automaticamente com autenticação JWT e Antiforgery.
 * @param {string} url - O endpoint da API.
 * @param {object} options - Opções do fetch (method, body, etc.).
 * @returns {Promise<any>} - A resposta da API em formato JSON.
 */
async function apiFetch(url, options = {}) {
    await ensureAntiforgeryToken();
    const headers = { ...options.headers };

    const jwtToken = localStorage.getItem('jwt_token');
    if (jwtToken) {
        headers['Authorization'] = `Bearer ${jwtToken}`;
    }

    // Lógica de cabeçalhos para POST/PUT/DELETE
    if (options.body && ['POST', 'PUT', 'DELETE'].includes(options.method)) {

    if (!(options.body instanceof FormData)) {
            headers['Content-Type'] = 'application/json';
        }

        // A lógica do Antiforgery continua aqui
        if (antiforgeryToken && antiforgeryHeaderName) {
            headers[antiforgeryHeaderName] = antiforgeryToken;
        }
    }

    const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include'
    });

    if (response.status === 204) return null;

    const contentType = response.headers.get("content-type");
    if (!contentType || !contentType.includes("json")) {
        const text = await response.text();
        console.error("Resposta inesperada do servidor:", text);
        if (response.status === 401) throw new Error('Não autorizado. Faça o login novamente.');
        throw new Error(`O servidor respondeu com um formato inesperado. Status: ${response.status}`);
    }

    const data = await response.json();

    if (!response.ok) {
        const errorMessage = data.message || `Erro na API. Status: ${response.status}`;
        throw new Error(errorMessage);
    }

    return data;
}

const apiService = {
    fetch: apiFetch,
    // Você pode adicionar helpers aqui no futuro se quiser, como:
    // get: (url) => apiFetch(url, { method: 'GET' }),
    // post: (url, body) => apiFetch(url, { method: 'POST', body: JSON.stringify(body) }),
};

export default apiService;