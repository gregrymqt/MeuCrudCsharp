// /js/admin/modules/api/adminAPI.js

// NOVO: Importa a função que busca o token. Ajuste o caminho se necessário.
import { getAuthToken } from '../../../../token/getTokens.js';

const cache = new Map();

/**
 * REATORADO: Função central que busca o token automaticamente.
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

    if (options.body && (options.method === 'POST' || options.method === 'PUT')) {
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
export const getPlans = (forceRefresh = false) => fetchAndCache('allPlans', '/api/admin/plans', { force: forceRefresh });
export const getPlanById = (id) => apiFetch(`/api/admin/plans/${id}`);
export const createPlan = (planData) => apiFetch('/api/admin/plans', { method: 'POST', body: JSON.stringify(planData) });
export const updatePlan = (id, planData) => apiFetch(`/api/admin/plans/${id}`, { method: 'PUT', body: JSON.stringify(planData) });
export const deletePlan = (id) => apiFetch(`/api/admin/plans/${id}`, { method: 'DELETE' });


// --- API de Cursos ---
export const getCourses = (forceRefresh = false) => fetchAndCache('allCourses', '/api/admin/courses', { force: forceRefresh });
export const createCourse = (courseData) => apiFetch('/api/admin/courses', { method: 'POST', body: JSON.stringify(courseData) });
export const updateCourse = (id, courseData) => apiFetch(`/api/admin/courses/${id}`, { method: 'PUT', body: JSON.stringify(courseData) });
export const deleteCourse = (id) => apiFetch(`/api/admin/courses/${id}`, { method: 'DELETE' });


// --- API de Alunos ---
export const getStudents = (forceRefresh = false) => fetchAndCache('allStudents', '/api/admin/students', { force: forceRefresh });


// --- API de Assinaturas ---
export const searchSubscription = (query) => apiFetch(`/api/admin/subscriptions/search?query=${encodeURIComponent(query)}`);
export const updateSubscriptionValue = (id, amount) => apiFetch(`/api/admin/subscriptions/${id}/value`, { method: 'PUT', body: JSON.stringify({ transactionAmount: amount }) });
export const updateSubscriptionStatus = (id, status) => apiFetch(`/api/admin/subscriptions/${id}/status`, { method: 'PUT', body: JSON.stringify({ status: status }) });


// --- Função de Cache ---
export function invalidateCache(cacheKey) {
    console.log(`Cache para a chave '${cacheKey}' foi invalidado.`);
    cache.delete(cacheKey);
}