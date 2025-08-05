document.addEventListener('DOMContentLoaded', function () {
    // Pega o elemento de vídeo. Se ele não existir (porque o vídeo não está disponível),
    // o script não faz nada.
    const videoElement = document.getElementById('videoPlayer');
    if (!videoElement) return;

    // --- PONTO DE INTEGRAÇÃO CORRIGIDO ---
    // Pega o ID do vídeo diretamente do modelo C#, que já foi validado pelo backend.
    // Isso é mais seguro e robusto do que ler da URL no frontend.
    const videoId = "@Model.VideoId";

    // Monta a URL para o manifesto no seu controller de streaming
    const manifestUrl = `/api/videos/${videoId}/manifest.m3u8`;

    // O resto do seu script HLS.js continua igual...
    if (Hls.isSupported()) {
        console.log("HLS.js é suportado. Inicializando player...");
        const hls = new Hls();
        hls.loadSource(manifestUrl);
        hls.attachMedia(videoElement);
        hls.on(Hls.Events.MANIFEST_PARSED, function () {
            console.log("Manifesto carregado.");
        });
        hls.on(Hls.Events.ERROR, function (event, data) {
            if (data.fatal) {
                console.error('Erro fatal do HLS.js:', data);
            }
        });
    }
    else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        console.log("Suporte nativo a HLS detectado.");
        videoElement.src = manifestUrl;
    } else {
        console.error("Este navegador não suporta a reprodução de vídeos HLS.");
    }
});