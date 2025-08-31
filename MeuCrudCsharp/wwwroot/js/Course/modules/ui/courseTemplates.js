// /js/modules/ui/courseTemplates.js

/**
 * Cria o HTML para um único card de vídeo.
 * @private // Função auxiliar, usada internamente por outras funções do módulo.
 * @param {object} video - O objeto do vídeo.
 * @param {number} courseId - O ID do curso ao qual o vídeo pertence.
 * @returns {string} A string HTML do card de vídeo.
 */
function createVideoCard(video, courseId) {
    // Tratamento robusto da duração
    let durationMinutes = 0;
    if (typeof video.duration === 'string' && video.duration.includes(':')) {
        const parts = video.duration.split(':');
        durationMinutes = parseInt(parts[1], 10) + (parseInt(parts[0], 10) * 60);
    } else if (typeof video.duration === 'number') {
        durationMinutes = Math.floor(video.duration / 60);
    }
    const durationText = `${durationMinutes} min`;

    const thumbnailUrl = video.thumbnailUrl || `https://placehold.co/600x400/111/FFFFFF?text=${encodeURIComponent(video.title)}`;
    const videoPageUrl = `/Videos/Index?videoId=${video.PublicId}&courseId=${courseId}`;

    return `
        <a href="${videoPageUrl}" class="video-card" data-video-id="${video.id}" data-course-id="${courseId}">
            <div class="video-thumbnail" style="background-image: url('${thumbnailUrl}')">
                <i class="fas fa-play play-icon"></i>
            </div>
            <div class="video-info">
                <h3 class="video-title">${video.title}</h3>
                <p class="video-duration">${durationText}</p>
            </div>
        </a>
    `;
}

/**
 * Renderiza as fileiras de cursos e seus vídeos em um container.
 * @param {Array<object>} courses - A lista de cursos a serem renderizados.
 * @param {HTMLElement} container - O elemento DOM onde o HTML será inserido.
 */
export function renderCourseRows(courses, container) {
    let coursesHTML = '';
    courses.forEach(course => {
        if (course.videos && course.videos.length > 0) {
            coursesHTML += `
                <div id="course-${course.id}" class="course-row">
                    <h2 class="course-row-title">${course.name}</h2>
                    <div class="videos-scroller">
                        ${course.videos.map(video => createVideoCard(video, course.id)).join('')}
                    </div>
                </div>
            `;
        }
    });
    container.insertAdjacentHTML('beforeend', coursesHTML);
}

/**
 * Renderiza os slides do carrossel principal em um container.
 * @param {Array<object>} courses - A lista de cursos para usar no carrossel.
 * @param {HTMLElement} container - O elemento DOM (swiper-wrapper) onde os slides serão inseridos.
 */
export function renderCarousel(courses, container) {
    const coursesWithVideos = courses.filter(course => course.videos && course.videos.length > 0);

    const slidesHTML = coursesWithVideos.map(course => {
        const firstVideo = course.videos[0];
        const thumbnailUrl = firstVideo.thumbnailUrl || `https://placehold.co/1280x720/000/FFF?text=${encodeURIComponent(course.name)}`;
        const videoPageUrl = `/Videos/Index?videoId=${firstVideo.PublicId}&courseId=${course.id}`;

        return `
            <a href="${videoPageUrl}" class="swiper-slide">
                <img src="${thumbnailUrl}" loading="lazy" alt="Banner para o curso ${course.name}" class="carousel-image"/>
                <div class="carousel-caption">
                    <h2 class="carousel-title">${course.name}</h2>
                    <p class="carousel-description">Comece a assistir agora!</p>
                </div>
            </a>
        `;
    }).join('');

    if (container) {
        container.innerHTML = slidesHTML;
    }
}

// Exportamos a função createVideoCard também, pois ela é necessária no módulo continueWatching
export { createVideoCard };