// /js/modules/api/coursesAPI.js

/**
 * Busca uma página de cursos da API.
 * @param {number} pageNumber - O número da página a ser buscada.
 * @param {number} pageSize - A quantidade de itens por página.
 * @returns {Promise<object>} Uma promessa que resolve com o resultado paginado ({ items, totalCount }).
 */
export async function fetchPaginatedCourses(pageNumber, pageSize) {
    const response = await fetch(`/api/courses/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}`);

    if (!response.ok) {
        throw new Error('Falha na comunicação com a rede ao buscar cursos.');
    }

    return await response.json();
}