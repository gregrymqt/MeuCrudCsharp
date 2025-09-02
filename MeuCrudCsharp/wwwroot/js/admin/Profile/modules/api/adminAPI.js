// /js/admin/modules/api/adminAPI.js

// NOVO: Importa a função que busca o token. Ajuste o caminho se necessário.

const cache = new Map();

let antiforgeryToken = null;
let antiforgeryHeaderName = null;

/**
 * Função para buscar o token Antiforgery do back-end na primeira vez.
 */
async function ensureAntiforgeryToken() {
    if (!antiforgeryToken) {
        try {
            // Chama o novo endpoint que criamos no back-end
            const response = await fetch('/api/antiforgery/token', { credentials: 'include' });
            if (response.ok) {
                const data = await response.json();
                antiforgeryToken = data.token;
                antiforgeryHeaderName = data.headerName;
                console.log("Token Antiforgery obtido com sucesso.");
            }
        } catch (error) {
            console.error("Falha ao obter o token Antiforgery:", error);
        }
    }
}

/**
 * REATORADO: Função central que busca o token automaticamente.
 * @param {string} url - O endpoint da API.
 * @param {object} options - Opções do fetch (method, body, etc.).
 * @returns {Promise<any>} - A resposta da API em formato JSON.
 */
async function apiFetch(url, options = {}) {
    await ensureAntiforgeryToken();

    const headers = { ...options.headers };

    // Adiciona o Content-Type para POST/PUT
    if (options.body && (options.method === 'POST' || options.method === 'PUT' || options.method === 'DELETE')) {
        headers['Content-Type'] = 'application/json';

        // ADICIONA O HEADER ANTIFORGERY AQUI!
        if (antiforgeryToken && antiforgeryHeaderName) {
            headers[antiforgeryHeaderName] = antiforgeryToken;
        }
    }

    const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include'
    });

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

/**
 * REATORADO: A função de cache agora usa o apiFetch automático.
 */
async function fetchAndCache(cacheKey, url, options = {}) {
    if (options.force || !cache.has(cacheKey)) {
        const data = await apiFetch(url, { ...options, method: 'GET' });
        cache.set(cacheKey, data);
        return data;
    }
    return cache.get(cacheKey);
}


// --- API de Planos ---
// REATORADO: As funções não precisam mais do parâmetro 'token'.
export const getPublicPlans = (forceRefresh = false) => fetchAndCache('allPublicPlans', '/api/public/plans', { force: forceRefresh });
export const getAdminPlans = (forceRefresh = false) => fetchAndCache('allAdminPlans', 'api/admin/plans', { force: forceRefresh });
export const getPlanById = (id) => apiFetch(`/api/admin/plans/${id}`);
export const createPlan = (planData) => apiFetch('/api/admin/plans', { method: 'POST', body: JSON.stringify(planData) });
export const updatePlan = (id, planData) => apiFetch(`/api/admin/plans/${id}`, { method: 'PUT', body: JSON.stringify(planData) });
export const deletePlan = (id) => apiFetch(`/api/admin/plans/${id}`, { method: 'DELETE' });


// --- API de Cursos ---
export const getCourses = (forceRefresh = false) => fetchAndCache('allCourses', '/api/admin/courses', { force: forceRefresh });
export const searchCoursesByName = (name) => apiFetch(`/api/admin/courses/search?name=${encodeURIComponent(name)}`);
export const createCourse = (courseData) => apiFetch('/api/admin/courses', { method: 'POST', body: JSON.stringify(courseData) });
export const updateCourse = (id, courseData) => apiFetch(`/api/admin/courses/${id}`, { method: 'PUT', body: JSON.stringify(courseData) });
export const deleteCourse = (id) => apiFetch(`/api/admin/courses/${id}`, { method: 'DELETE' });


// --- API de Alunos ---
export const getStudents = (forceRefresh = false) => fetchAndCache('allStudents', '/api/admin/students', { force: forceRefresh });


// --- API de Assinaturas ---
export const searchSubscription = (query) => apiFetch(`/api/admin/subscriptions/subscriptions/search?query=${encodeURIComponent(query)}`);
export const updateSubscriptionValue = (id, amount) => apiFetch(`/api/admin/subscriptions/${id}/value`, { method: 'PUT', body: JSON.stringify({ transactionAmount: amount }) });
export const updateSubscriptionStatus = (id, status) => apiFetch(`/api/admin/subscriptions/${id}/status`, { method: 'PUT', body: JSON.stringify({ status: status }) });


// --- Função de Cache ---
export function invalidateCache(cacheKey) {
    console.log(`Cache para a chave '${cacheKey}' foi invalidado.`);
    cache.delete(cacheKey);
}