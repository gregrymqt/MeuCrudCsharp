// /js/services/videoPlayerService.js

/**
 * Inicializa e toca um stream de vídeo HLS em um elemento <video>.
 * @param {HTMLVideoElement} videoElement - O elemento <video> onde o stream será tocado.
 * @param {string} videoId - O identificador único do vídeo para construir a URL do manifesto.
 */
export function initializeAndPlayStream(videoElement, videoId) {
    const manifestUrl = `/api/videos/${videoId}/manifest.m3u8`;

    if (Hls.isSupported()) {
        const hls = new Hls({
            // Configurações opcionais para melhor performance
            startLevel: -1, // Começa com a melhor qualidade possível
            capLevelToPlayerSize: true,
        });
        hls.loadSource(manifestUrl);
        hls.attachMedia(videoElement);
        hls.on(Hls.Events.MANIFEST_PARSED, () => {
            console.log("HLS manifest carregado, iniciando a reprodução.");
            videoElement.play().catch(error => {
                console.error("Erro ao tentar tocar o vídeo automaticamente:", error);
                // Navegadores modernos podem bloquear o autoplay se não houver interação do usuário.
                // O clique no placeholder já conta como uma interação, então isso geralmente funciona.
            });
        });
        hls.on(Hls.Events.ERROR, function (event, data) {
            if (data.fatal) {
                console.error('Erro fatal no HLS.js:', data);
            }
        });
    } else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        // Suporte nativo do Safari e iOS
        videoElement.src = manifestUrl;
        videoElement.addEventListener('loadedmetadata', () => {
            videoElement.play();
        });
    } else {
        console.error("Este navegador não suporta a reprodução de vídeo HLS.");
    }
}