// js/modules/api/adminAPI.js (ou o caminho correto)

// 1. IMPORTA os serviços centrais. Toda a lógica de fetch e cache virá daqui.
import apiService from '../../../../core/apiService.js';
import cacheService from '../../../../core/cacheService.js';

// ==========================================================================================
// FUNÇÃO HELPER LOCAL (só para este arquivo)
// ==========================================================================================

/**
 * Helper para buscar dados, com lógica de cache.
 * Usa os serviços centrais importados.
 * @param {string} cacheKey A chave para o cache.
 * @param {string} url O endpoint da API.
 * @param {boolean} forceRefresh Se true, ignora o cache e busca novos dados.
 * @returns {Promise<any>}
 */
async function fetchAndCache(cacheKey, url, forceRefresh = false) {
    if (!forceRefresh) {
        const cachedData = cacheService.get(cacheKey);
        if (cachedData) {
            return cachedData;
        }
    }

    // Se não houver cache ou se forçar a atualização, busca da API
    const data = await apiService.fetch(url);
    cacheService.set(cacheKey, data); // Salva os novos dados no cache
    return data;
}

// ==========================================================================================
// API DE PLANOS (Plans)
// ==========================================================================================

export const getPublicPlans = (page =1, pageSize = 10 ,forceRefresh = false) => fetchAndCache('allPublicPlans', `/api/public/plans?page=${page}&${pageSize}`, forceRefresh);
export const getAdminPlans = (forceRefresh = false) => fetchAndCache('allAdminPlans', '/api/admin/plans', forceRefresh);
export const getPlanById = (id) => apiService.fetch(`/api/admin/plans/${id}`);
export const createPlan = (planData) => apiService.fetch('/api/admin/plans', { method: 'POST', body: JSON.stringify(planData) });
export const updatePlan = (id, planData) => apiService.fetch(`/api/admin/plans/${id}`, { method: 'PUT', body: JSON.stringify(planData) });
export const deletePlan = (id) => apiService.fetch(`/api/admin/plans/${id}`, { method: 'DELETE' });
// ==========================================================================================
// API DE CURSOS (Courses)
// ==========================================================================================

export const getCourses = (forceRefresh = false) => fetchAndCache('allCourses', '/api/admin/courses', forceRefresh);
export const getCoursesPublicId = (id) => apiService.fetch(`/api/admin/courses/${id}`);
export const searchCoursesByName = (name) => apiService.fetch(`/api/admin/courses/search?name=${encodeURIComponent(name)}`);
export const createCourse = (courseData) => apiService.fetch('/api/admin/courses', { method: 'POST', body: JSON.stringify(courseData) });
export const updateCourse = (id, courseData) => apiService.fetch(`/api/admin/courses/${id}`, { method: 'PUT', body: JSON.stringify(courseData) });
export const deleteCourse = (id) => apiService.fetch(`/api/admin/courses/${id}`, { method: 'DELETE' });

// ==========================================================================================
// API DE ALUNOS (Students)
// ==========================================================================================

export const getStudents = (forceRefresh = false) => fetchAndCache('allStudents', '/api/admin/students?page=${page}&${pageSize}`', forceRefresh);
export const getStudentsPublicId = (id) => apiService.fetch(`/api/admin/students/${id}`);

// ==========================================================================================
// API DE ASSINATURAS (Subscriptions)
// ==========================================================================================

export const searchSubscription = (query) => apiService.fetch(`/api/admin/subscriptions/search?query=${encodeURIComponent(query)}`);
export const updateSubscriptionValue = (id, amount) => apiService.fetch(`/api/admin/subscriptions/${id}/value`, { method: 'PUT', body: JSON.stringify({ transactionAmount: amount }) });
export const updateSubscriptionStatus = (id, status) => apiService.fetch(`/api/admin/subscriptions/${id}/status`, { method: 'PUT', body: JSON.stringify({ status: status }) });

/**
 * Invalida (remove) um item específico do cache.
 * @param {string} cacheKey A chave a ser removida.
 */
export function invalidateCache(cacheKey) {
    console.log(`Cache para a chave '${cacheKey}' foi invalidado.`);
    // Usa o método 'remove' do nosso cacheService central.
    cacheService.remove(cacheKey);
}