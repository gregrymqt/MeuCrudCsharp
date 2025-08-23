// /js/admin/videos/modules/services/signalRService.js
export function createVideoHubConnection(callbacks) {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/videoProcessingHub")
        .build();

    connection.on("ProgressUpdate", callbacks.onProgress);

    return {
        start: () => connection.start(),
        stop: () => connection.stop(),
        subscribe: (jobId) => connection.invoke("SubscribeToJobProgress", jobId),
    };
}s