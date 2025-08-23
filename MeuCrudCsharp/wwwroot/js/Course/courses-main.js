// /js/courses-main.js

import { fetchPaginatedCourses } from './modules/api/coursesAPI.js';
import { renderCourseRows, renderCarousel } from './modules/ui/courseTemplates.js';
import { renderContinueWatchingSection } from './modules/ui/continueWatching.js';
import { initializeCarousel } from './modules/services/swiperService.js';

// --- Estado da Página ---
let currentPage = 1;
const pageSize = 5;
let isLoading = false;
let hasMorePages = true;

// --- Seletores de DOM ---
const carouselWrapper = document.getElementById('carousel-wrapper');
const coursesContainer = document.getElementById('courses-section-container');
const pageLoader = document.getElementById('page-loader');

/**
 * Função principal que busca e renderiza os cursos.
 */
async function loadCourses() {
    if (isLoading || !hasMorePages) return;
    isLoading = true;
    pageLoader.style.display = 'block';

    try {
        const paginatedResult = await fetchPaginatedCourses(currentPage, pageSize);
        const { items, totalCount } = paginatedResult;

        if (items && items.length > 0) {
            if (currentPage === 1) {
                renderCarousel(items, carouselWrapper);
                initializeCarousel();
            }
            renderCourseRows(items, coursesContainer);
            currentPage++;
            hasMorePages = (currentPage - 1) * pageSize < totalCount;
        } else {
            hasMorePages = false;
        }
    } catch (error) {
        console.error("Falha ao carregar cursos:", error);
        coursesContainer.insertAdjacentHTML('beforeend', `<p class="error-message">Erro ao carregar mais cursos.</p>`);
    } finally {
        isLoading = false;
        pageLoader.style.display = 'none';
    }
}

/**
 * Configura o observador para o scroll infinito.
 */
function setupInfiniteScroll() {
    const observer = new IntersectionObserver((entries) => {
        if (entries[0].isIntersecting && hasMorePages) {
            loadCourses();
        }
    }, { rootMargin: '200px' }); // Carrega 200px antes de chegar no final

    observer.observe(pageLoader);
}

/**
 * Função de inicialização da página.
 */
function initPage() {
    renderContinueWatchingSection(coursesContainer);
    loadCourses(); // Carrega a primeira leva de cursos
    setupInfiniteScroll();
}

// Ponto de entrada
document.addEventListener('DOMContentLoaded', initPage);