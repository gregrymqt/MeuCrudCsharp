// /js/admin/videos/modules/api/videosAPI.js
export const getPaginatedVideos = (page, pageSize = 15) => fetch(`/api/admin/videos?page=${page}&pageSize=${pageSize}`).then(handleResponse);
export const uploadVideoFile = (formData) => fetch('/api/admin/videos/upload', { method: 'POST', body: formData }).then(handleResponse);
export const saveVideoMetadata = (formData) => fetch('/api/admin/videos', { method: 'POST', body: formData }).then(handleResponse);
export const updateVideoMetadata = (id, formData) => fetch(`/api/admin/videos/${id}`, { method: 'PUT', body: formData }).then(handleResponse);
export const deleteVideo = (id) => fetch(`/api/admin/videos/${id}`, { method: 'DELETE' }).then(handleResponse);
export const getCourses = () => fetch('/api/admin/courses').then(handleResponse);

async function handleResponse(response) {
    if (!response.ok) {
        const errorData = await response.json().catch(() => ({ message: 'Erro desconhecido.' }));
        throw new Error(errorData.message);
    }
    return response.json();
}