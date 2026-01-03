import { ApiService } from "../../../../shared/services/api.service";
import type { Video } from "../../../../types/models";
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
  create: async (data: CreateVideoParams): Promise<Video> => {
    const { videoFile, thumbnailFile, ...dto } = data;

    // Cria um array apenas com os arquivos que existem (não nulos)
    const files: File[] = [];
    if (videoFile) files.push(videoFile);
    if (thumbnailFile) files.push(thumbnailFile);

    // Enviamos o array 'files'.
    // O ApiService detectará se o vídeo é grande e usará o SmartHandler automaticamente.
    return await ApiService.postWithFile<Video, typeof dto>(
      BASE_ENDPOINT,
      dto,
      files,
      "files" // Backend deve esperar: public List<IFormFile> files { get; set; }
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
