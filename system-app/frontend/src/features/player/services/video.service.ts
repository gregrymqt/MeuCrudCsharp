import { ApiService } from "../../../shared/services/api.service";
import type { VideoDto } from "../types/player.type";

export const videoService = {
  /**
   * Busca os metadados do vídeo (Título, Descrição, ID do Storage)
   * Assume que existe um endpoint GET /api/videos/{publicId}
   */
  getById: async (publicId: string): Promise<VideoDto> => {
    return await ApiService.get<VideoDto>(`/videos/${publicId}`);
  },

  /**
   * Constrói a URL do manifesto HLS baseada no controller C# [cite: 21]
   */
  getManifestUrl: (storageIdentifier: string): string => {
    // Rota definida no C#: [HttpGet("{storageIdentifier}/manifest.m3u8")]
    return `/api/videos/${storageIdentifier}/manifest.m3u8`;
  }
};