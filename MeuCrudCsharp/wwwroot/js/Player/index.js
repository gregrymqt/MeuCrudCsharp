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
document.addEventListener('DOMContentLoaded', function() {
    /**
     * The HTML <video> element where the player will be rendered.
     * @type {HTMLVideoElement|null}
     */
    const videoElement = document.getElementById('videoPlayer');

    // If the video element does not exist on the page, the script stops its execution
    // to prevent errors on pages that do not contain a player.
    if (!videoElement) return;

    /**
     * The unique identifier for the video, injected by the backend (ASP.NET Core Razor).
     * @type {string}
     * @description Using `@Model.VideoId` is a secure practice that ensures the video ID
     * has been validated on the server, preventing client-side manipulation of the ID in the URL.
     */
    const videoId = "@Model.VideoId";

    /**
     * The HLS manifest URL (.m3u8) that will be loaded into the player.
     * It points to a backend streaming API endpoint.
     * @type {string}
     */
    const manifestUrl = `/api/videos/${videoId}/manifest.m3u8`;

    // Check if the current browser supports the HLS.js library.
    // This is the preferred approach to ensure cross-browser compatibility.
    if (Hls.isSupported()) {
        console.log("HLS.js is supported. Initializing player...");
        const hls = new Hls();
        hls.loadSource(manifestUrl);
        hls.attachMedia(videoElement);
        // Event fired when the manifest is successfully loaded and parsed.
        hls.on(Hls.Events.MANIFEST_PARSED, function() {
            console.log("HLS manifest loaded and ready for playback.");
        });
        // Event to capture and log errors that may occur during streaming.
        hls.on(Hls.Events.ERROR, function(event, data) {
            if (data.fatal) {
                console.error('Fatal HLS.js error:', data);
            }
        });
    } // As a fallback, check if the browser has native HLS support.
    // This is common in Safari (macOS and iOS).
    else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
        console.log("Native HLS support detected. Using the browser's player.");
        videoElement.src = manifestUrl;
    } else {
        // If neither HLS.js nor native support is available, inform the developer.
        console.error("This browser does not support HLS video playback.");
    }
});