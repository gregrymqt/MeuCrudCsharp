// /js/modules/ui/continueWatching.js
import { createVideoCard } from './courseTemplates.js';

/**
 * Verifica o localStorage e renderiza a seção "Continue Assistindo" se houver dados.
 * @param {HTMLElement} container - O elemento DOM principal onde a seção será inserida.
 */
export function renderContinueWatchingSection(container) {
    const lastWatchedVideoJson = localStorage.getItem('lastWatchedVideo');
    if (!lastWatchedVideoJson) return;

    try {
        const videoData = JSON.parse(lastWatchedVideoJson);
        if (!videoData || !videoData.courseId) return;

        const continueWatchingHTML = `
            <div id="continue-watching-row" class="course-row">
                <h2 class="course-row-title">Continue Assistindo</h2>
                <div class="videos-scroller">
                    ${createVideoCard(videoData, videoData.courseId)}
                </div>
            </div>
        `;
        container.insertAdjacentHTML('afterbegin', continueWatchingHTML);
    } catch (e) {
        console.error("Erro ao processar dados de 'Continue Assistindo':", e);
        localStorage.removeItem('lastWatchedVideo');
    }
}