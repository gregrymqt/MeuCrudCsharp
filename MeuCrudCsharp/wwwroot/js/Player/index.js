/**
 * @file Script for initializing the HLS (HTTP Live Streaming) video player.
 *
 * This script runs when the DOM is fully loaded. It identifies the video element,
 * constructs the HLS manifest URL using a video ID provided by the backend (via Razor),
 * and then attempts to initialize playback using the HLS.js library.
 *
 * If HLS.js is not supported, it checks for native browser support (common on Apple devices)
 * as a fallback. If neither option is available, an error is logged to the console.
 */
document.addEventListener('DOMContentLoaded', function () {
    const videoContainer = document.getElementById('videoContainer'); // O container geral
    const placeholder = document.getElementById('videoPlaceholder');

    // Se o placeholder não existir, não faz nada.
    if (!placeholder) return;

    // 1. Adiciona um ouvinte de evento de clique ao placeholder
    placeholder.addEventListener('click', function () {
        // Pega o ID do vídeo a partir do atributo data-*
        const videoId = placeholder.getAttribute('data-video-id');

        // Remove o placeholder da tela
        placeholder.style.display = 'none';

        // 2. Cria o elemento <video> dinamicamente
        const videoElement = document.createElement('video');
        videoElement.id = 'videoPlayer';
        videoElement.controls = true;
        videoElement.autoplay = true; // Importante para começar a tocar após o clique

        // Adiciona o novo elemento de vídeo ao container
        videoContainer.prepend(videoElement); // Adiciona no início do container

        // 3. Executa a lógica de inicialização do HLS.js que você já tinha
        initializePlayer(videoElement, videoId);

    }, { once: true }); // O { once: true } remove o ouvinte após o primeiro clique.

    /**
     * Função que contém a sua lógica original de inicialização do player.
     * @param {HTMLVideoElement} videoEl - O elemento de vídeo recém-criado.
     * @param {string} videoId - O ID do vídeo para montar a URL.
     */
    function initializePlayer(videoEl, videoId) {
        const manifestUrl = `/api/videos/${videoId}/manifest.m3u8`;

        if (Hls.isSupported()) {
            const hls = new Hls();
            hls.loadSource(manifestUrl);
            hls.attachMedia(videoEl);
            hls.on(Hls.Events.MANIFEST_PARSED, function () {
                console.log("HLS manifest loaded, starting playback.");
                videoEl.play(); // Garante o início da reprodução
            });
            // ... seu código de tratamento de erro ...
        } else if (videoEl.canPlayType('application/vnd.apple.mpegurl')) {
            videoEl.src = manifestUrl;
            videoEl.play(); // Garante o início da reprodução
        } else {
            console.error("This browser does not support HLS video playback.");
        }
    }
});