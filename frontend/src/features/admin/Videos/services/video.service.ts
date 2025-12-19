// src/features/admin/videos/services/video.service.ts

import { ApiService } from "../../../../shared/services/api.service";
import type { Video } from "../../../../types/models";
import type {
  VideoFilters,
  PaginatedResponse,
  CreateVideoParams,
  UpdateVideoParams,
} from "../types/video-manager.types";

const BASE_ENDPOINT = "/admin/videos"; //

export const VideoService = {
  // GET: Listar vídeos paginados
  getAll: async (filters: VideoFilters): Promise<PaginatedResponse<Video>> => {
    const params = new URLSearchParams({
      page: filters.page.toString(),
      pageSize: filters.pageSize.toString(),
    });

    return await ApiService.get<PaginatedResponse<Video>>(
      `${BASE_ENDPOINT}?${params.toString()}`
    );
  },

  // POST: Upload de Vídeo
  create: async (data: CreateVideoParams): Promise<Video> => {
    const formData = new FormData();

    // Anexando campos de texto
    formData.append("title", data.title);
    formData.append("description", data.description);
    formData.append("courseId", data.courseId);

    // Anexando Arquivos (IFormFile no C#)
    formData.append("videoFile", data.videoFile);

    if (data.thumbnailFile) {
      formData.append("thumbnailFile", data.thumbnailFile);
    }

    // Usa o método específico para FormData do seu ApiService
    return await ApiService.postFormData<Video>(BASE_ENDPOINT, formData);
  },

  // PUT: Atualizar Vídeo
  update: async (id: string, data: UpdateVideoParams): Promise<Video> => {
    const formData = new FormData();

    formData.append("title", data.title);
    formData.append("description", data.description);

    // Apenas anexa se houver nova thumbnail
    if (data.thumbnailFile) {
      formData.append("thumbnailFile", data.thumbnailFile);
    }

    // Usa o método específico para FormData do seu ApiService
    return await ApiService.putFormData<Video>(
      `${BASE_ENDPOINT}/${id}`,
      formData
    );
  },

  // DELETE: Remover Vídeo
  delete: async (id: string): Promise<void> => {
    await ApiService.delete(`${BASE_ENDPOINT}/${id}`);
  },
};
