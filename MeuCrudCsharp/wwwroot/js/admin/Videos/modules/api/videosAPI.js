// /js/admin/videos/modules/api/videosAPI.js

// NOVO: Importa a função que busca o token. Ajuste o caminho se necessário.
import { getAuthToken } from '../../../../token/getTokens.js';

/**
 * REATORADO: Função central que busca o token automaticamente.
 * Lida com a adição de token, tratamento de erros, verificação de JSON,
 * e é inteligente para não definir 'Content-Type' para FormData.
 * @param {string} url - O endpoint da API.
 * @param {object} options - Opções do fetch (method, body, etc.).
 * @returns {Promise<any>} - A resposta da API em formato JSON.
 */
async function apiFetch(url, options = {}) {
    // 1. Pega o token automaticamente
    const token = getAuthToken();

    // 2. Configura os cabeçalhos
    const headers = { ...options.headers };
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    // IMPORTANTE: Não definimos 'Content-Type' quando o corpo é FormData.
    if (options.body && !(options.body instanceof FormData)) {
        headers['Content-Type'] = 'application/json';
    }

    // 3. Realiza a chamada fetch
    const response = await fetch(url, { ...options, headers });

    // 4. Lida com a resposta
    if (response.status === 204) {
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

// --- API de Vídeos ---
// REATORADO: As funções agora não precisam mais do parâmetro 'token'.
export const getPaginatedVideos = (page, pageSize = 15) => apiFetch(`/api/admin/videos?page=${page}&pageSize=${pageSize}`);
export const uploadVideoFile = (formData) => apiFetch('/api/admin/videos/upload', { method: 'POST', body: formData });
export const saveVideoMetadata = (formData) => apiFetch('/api/admin/videos', { method: 'POST', body: formData });
export const updateVideoMetadata = (id, formData) => apiFetch(`/api/admin/videos/${id}`, { method: 'PUT', body: formData });
export const deleteVideo = (id) => apiFetch(`/api/admin/videos/${id}`, { method: 'DELETE' });

// --- API de Cursos (utilizada neste módulo) ---
// REATORADO: Também não precisa mais do parâmetro 'token'.
export const getCourses = () => apiFetch('/api/admin/courses');