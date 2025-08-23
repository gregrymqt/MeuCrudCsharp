// /js/admin/modules/api/adminAPI.js

const cache = new Map();

// Função de cache refatorada para apenas retornar dados
async function fetchAndCache(cacheKey, url, options = {}) {
    // Se a opção 'force: true' for passada, ignora o cache
    if (options.force || !cache.has(cacheKey)) {
        const response = await fetch(url, options);
        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: `HTTP error! Status: ${response.status}` }));
            throw new Error(errorData.message);
        }
        const data = await response.json();
        cache.set(cacheKey, data);
        return data;
    }
    return cache.get(cacheKey);
}

// --- API de Planos ---
export const getPlans = () => fetchAndCache('allPlans', '/api/admin/plans');

export const getPlanById = (id) => fetch(`/api/admin/plans/${id}`).then(res => res.json());

export const createPlan = (planData) => fetch('/api/admin/plans', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(planData)
}).then(res => res.json());

export const updatePlan = (id, planData) => fetch(`/api/admin/plans/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(planData)
}).then(res => res.json());

export const deletePlan = (id) => fetch(`/api/admin/plans/${id}`, { method: 'DELETE' });

// --- API de Cursos ---
export const getCourses = () => fetchAndCache('allCourses', '/api/admin/courses');

export const createCourse = (courseData) => fetch('/api/admin/courses', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(courseData)
}).then(res => res.json());

export const updateCourse = (id, courseData) => fetch(`/api/admin/courses/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(courseData)
}).then(res => res.json());

export const deleteCourse = (id) => fetch(`/api/admin/courses/${id}`, { method: 'DELETE' });

// --- API de Alunos ---
export const getStudents = () => fetchAndCache('allStudents', '/api/admin/students');

// --- API de Assinaturas ---
export const searchSubscription = (query) => fetch(`/api/admin/subscriptions/search?query=${encodeURIComponent(query)}`).then(res => res.json());

export const updateSubscriptionValue = (id, amount) => fetch(`/api/admin/subscriptions/${id}/value`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ transactionAmount: amount })
}).then(res => res.json());

export const updateSubscriptionStatus = (id, status) => fetch(`/api/admin/subscriptions/${id}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status: status })
}).then(res => res.json());

// Função para invalidar um cache específico, útil após criar/editar/excluir
export function invalidateCache(cacheKey) {
    cache.delete(cacheKey);
}