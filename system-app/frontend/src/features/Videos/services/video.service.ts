import { ApiService } from "src/shared/services/api.service";
import type { Video } from "src/types/models";
import type {
  VideoFilters,
  PaginatedResponse,
  CreateVideoParams,
  UpdateVideoParams,
} from "../types/video-manager.types";

const BASE_ENDPOINT = "/admin/videos";

export const VideoService = {
  // GET
  getAll: async (filters: VideoFilters): Promise<PaginatedResponse<Video>> => {
    const params = new URLSearchParams({
      page: filters.page.toString(),
      pageSize: filters.pageSize.toString(),
    });

    return await ApiService.get<PaginatedResponse<Video>>(
      `${BASE_ENDPOINT}?${params.toString()}`
    );
  },

  // POST - Criação (Pode conter Vídeo Grande + Thumbnail)
  // VideoService.ts

  create: async (data: CreateVideoParams): Promise<Video> => {
    // Separa os arquivos do DTO de texto
    const { videoFile, thumbnailFile, ...textDto } = data;

    const formData = new FormData();
    // Campos de Texto
    formData.append("Title", textDto.title);
    formData.append("Description", textDto.description);
    formData.append("CourseId", textDto.courseId); // Backend precisa disso!

    // Arquivos
    if (videoFile) {
      // Chave 'File' para casar com BaseUploadDto no C#
      formData.append("File", videoFile);
    }

    if (thumbnailFile) {
      // Chave 'ThumbnailFile' para casar com CreateVideoDto no C#
      formData.append("ThumbnailFile", thumbnailFile);
    }

    // Como o chunking é complexo, recomendo usar a sua ApiService
    // mas passando o nome da chave do arquivo principal como 'File'

    return await ApiService.postWithFile<Video, any>(
      BASE_ENDPOINT,
      { ...textDto, ThumbnailFile: thumbnailFile }, // Passa a Thumb como "dado" se o handler suportar File em objeto
      videoFile, // O arquivo principal (Vídeo) que será fatiado
      "File" // Nome da chave no Backend (BaseUploadDto.File)
    );
  },

  // PUT - Atualização
  update: async (id: string, data: UpdateVideoParams): Promise<Video> => {
    const { thumbnailFile, ...dto } = data;

    // Na atualização, geralmente só atualizamos a thumbnail ou dados de texto
    const file = thumbnailFile || null;

    return await ApiService.putWithFile<Video, typeof dto>(
      `${BASE_ENDPOINT}/${id}`,
      dto,
      file,
      "thumbnailFile" // Se for só a thumb, podemos usar o nome específico se o backend exigir
    );
  },

  // DELETE
  delete: async (id: string): Promise<void> => {
    await ApiService.delete(`${BASE_ENDPOINT}/${id}`);
  },
};
