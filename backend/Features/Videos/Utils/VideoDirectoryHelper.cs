using System.IO;

namespace MeuCrudCsharp.Features.Videos.Utils
{
    public static class VideoDirectoryHelper
    {
        /// <summary>
        /// Apaga a pasta de processamento HLS específica do vídeo.
        /// Caminho: wwwroot/uploads/Videos/{storageIdentifier}
        /// </summary>
        public static void DeleteHlsFolder(string webRootPath, string storageIdentifier)
        {
            if (string.IsNullOrEmpty(webRootPath) || string.IsNullOrEmpty(storageIdentifier))
                return;

            // Monta o caminho: wwwroot/uploads/Videos/GUID-DO-VIDEO
            string hlsFolderPath = Path.Combine(webRootPath, "uploads", "Videos", storageIdentifier);

            if (Directory.Exists(hlsFolderPath))
            {
                // O true indica recursividade (apaga subpastas e arquivos dentro)
                Directory.Delete(hlsFolderPath, true); 
            }
        }
    }
}