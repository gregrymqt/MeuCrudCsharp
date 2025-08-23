// /js/video-player-main.js

import { initializeAndPlayStream } from './services/videoPlayerService.js';

/**
 * Inicializa a lógica da página do player de vídeo.
 */
function initializePage() {
    const videoContainer = document.getElementById('videoContainer');
    const placeholder = document.getElementById('videoPlaceholder');

    if (!placeholder) return;

    // Adiciona o evento de clique ao placeholder.
    // { once: true } garante que este evento só dispare uma vez.
    placeholder.addEventListener('click', () => {
        // 1. Pega os dados necessários do placeholder
        const videoId = placeholder.getAttribute('data-video-id');
        if (!videoId) {
            console.error('Atributo data-video-id não encontrado no placeholder.');
            return;
        }

        // 2. Esconde o placeholder
        placeholder.style.display = 'none';

        // 3. Cria o elemento <video> dinamicamente
        const videoElement = document.createElement('video');
        videoElement.id = 'videoPlayer';
        videoElement.controls = true;
        videoElement.autoplay = true;
        videoElement.playsInline = true; // Essencial para boa experiência em mobile

        // Adiciona o elemento de vídeo ao seu container
        videoContainer.prepend(videoElement);

        // 4. Usa o serviço para carregar e tocar o vídeo
        initializeAndPlayStream(videoElement, videoId);

    }, { once: true });
}

// Garante que o script rode após o carregamento do HTML
document.addEventListener('DOMContentLoaded', initializePage);