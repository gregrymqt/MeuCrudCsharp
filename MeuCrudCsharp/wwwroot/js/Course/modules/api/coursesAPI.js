// /js/modules/api/coursesAPI.js

/**
 * NOVO: Função central e automática para todas as chamadas de API deste módulo.
 * Pega o token automaticamente e lida com toda a lógica de requisição e resposta.
 * @param {string} url - O endpoint da API.
 * @param {object} options - Opções do fetch (method, body, etc.).
 * @returns {Promise<any>} - A resposta da API em formato JSON.
 */
async function apiFetch(url, options = {}) {
    // 2. Configura os cabeçalhos.
    const headers = { ...options.headers };

    // Adiciona Content-Type automaticamente se houver corpo na requisição
    if (options.body && (options.method === 'POST' || options.method === 'PUT')) {
        headers['Content-Type'] = 'application/json';
    }

    // 4. Realiza a chamada fetch
    const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include',
    });

    // 5. Lida com a resposta (lógica robusta que já usamos)
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
 * REATORADO: Busca uma página de cursos da API.
 * A chamada agora é uma linha simples que usa nossa função central automática.
 * @param {number} pageNumber - O número da página a ser buscada.
 * @param {number} pageSize - A quantidade de itens por página.
 * @returns {Promise<object>} Uma promessa que resolve com o resultado paginado.
 */
export function fetchPaginatedCourses(pageNumber, pageSize) {
    return apiFetch(`/api/courses/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}`);
}