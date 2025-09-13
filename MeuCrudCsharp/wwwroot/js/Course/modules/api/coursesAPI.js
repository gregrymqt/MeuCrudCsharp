// /js/modules/api/coursesAPI.js

// 1. IMPORTA o serviço central de API.
//    Toda a lógica de autenticação, cabeçalhos e tratamento de erros virá daqui.
import apiService from '../../../core/apiService.js'; // Ajuste o caminho se sua estrutura de pastas for diferente

/**
 * Busca uma página de cursos da API pública.
 * A chamada agora é uma linha simples que usa o apiService central.
 * @param {number} pageNumber - O número da página a ser buscada.
 * @param {number} pageSize - A quantidade de itens por página.
 * @returns {Promise<object>} Uma promessa que resolve com o resultado paginado.
 */
export function fetchPaginatedCourses(pageNumber, pageSize) {
    // 2. USA o apiService.fetch importado para fazer a chamada.
    return apiService.fetch(`/api/public/courses/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}`);
}

// Você pode adicionar outras funções de API relacionadas a cursos aqui, seguindo o mesmo padrão.
// Exemplo:
/*
export function getCourseDetails(courseId) {
    return apiService.fetch(`/api/public/courses/${courseId}`);
}
*/