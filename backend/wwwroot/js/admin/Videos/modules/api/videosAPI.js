// /js/admin/videos/modules/api/videosAPI.js

// 1. IMPORTA os serviços centrais.
import apiService from '../../../../core/apiService.js'; // Ajuste o caminho se necessário
import cacheService from '../../../../core/cacheService.js'; // Mesmo que não use, é bom manter o padrão

// ==========================================================================================
// API DE VÍDEOS (Videos)
// ==========================================================================================

export const getPaginatedVideos = (page, pageSize = 15) => {
    // Usa o apiService que agora sabe como lidar com todos os tipos de requisição.
    return apiService.fetch(`/api/admin/videos?page=${page}&pageSize=${pageSize}`);
};

export const uploadVideoFile = (formData) => {
    // Passa o formData diretamente. O apiService saberá como lidar com o cabeçalho.
    return apiService.fetch('/api/admin/videos/upload', {
        method: 'POST',
        body: formData
    });
};

export const saveVideoMetadata = (videoData) => {
    return apiService.fetch('/api/admin/videos', {
        method: 'POST',
        body: JSON.stringify(videoData) // Aqui usamos JSON
    });
};

export const updateVideoMetadata = (id, videoData) => {
    return apiService.fetch(`/api/admin/videos/${id}`, {
        method: 'PUT',
        body: JSON.stringify(videoData) // E aqui também
    });
};

export const deleteVideo = (id) => {
    return apiService.fetch(`/api/admin/videos/${id}`, {
        method: 'DELETE'
    });
};

// ==========================================================================================
// API DE CURSOS (Usada para preencher selects, por exemplo)
// ==========================================================================================

export const getCourses = () => {
    const CACHE_KEY = 'allCoursesForVideoModule'; // Use uma chave de cache se apropriado
    let data = cacheService.get(CACHE_KEY);
    if(data) return Promise.resolve(data);

    return apiService.fetch('/api/admin/courses').then(courses => {
        cacheService.set(CACHE_KEY, courses);
        return courses;
    });
};