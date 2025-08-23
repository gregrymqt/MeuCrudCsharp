// /js/admin/videos/modules/services/videoPlayerService.js
let hlsInstance;

export function initializePlayer(videoElement) {
    if (hlsInstance) hlsInstance.destroy();
    if (Hls.isSupported()) {
        hlsInstance = new Hls();
        hlsInstance.attachMedia(videoElement);
    }
}

export function playStream(storageId, videoElement) {
    const manifestUrl = `/api/videos/${storageId}/manifest.m3u8`;
    if (hlsInstance) {
        hlsInstance.loadSource(manifestUrl);
        hlsInstance.on(Hls.Events.MANIFEST_PARSED, () => videoElement.play());
    } else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        videoElement.src = manifestUrl;
        videoElement.play();
    }
}

export function destroyPlayer() {
    if (hlsInstance) {
        hlsInstance.destroy();
        hlsInstance = null;
    }
}