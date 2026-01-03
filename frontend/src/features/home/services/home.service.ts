import { ApiService } from "../../../shared/services/api.service";
import type {
  HomeContent,
  HeroSlideData,
  ServiceData,
  HeroFormValues,
  ServiceFormValues,
} from "../types/home.types";

const ENDPOINT = "/Home";

export const HomeService = {
  // GET (Padrão)
  getHomeContent: async (): Promise<HomeContent> => {
    return await ApiService.get<HomeContent>(`${ENDPOINT}`);
  },

  // --- HERO SLIDES (Com Upload) ---

  createHero: async (data: HeroFormValues): Promise<HeroSlideData> => {
    // Desestrutura para separar o FileList do DTO
    const { newImage, ...dto } = data;

    // Pega o arquivo físico se existir
    const file = newImage && newImage.length > 0 ? newImage[0] : null;

    return await ApiService.postWithFile<HeroSlideData, typeof dto>(
      `${ENDPOINT}/hero`,
      dto,
      file,
      "file" // Backend: IFormFile file
    );
  },

  updateHero: async (id: number, data: HeroFormValues): Promise<void> => {
    const { newImage, ...dto } = data;
    const file = newImage && newImage.length > 0 ? newImage[0] : null;

    return await ApiService.putWithFile<void, typeof dto>(
      `${ENDPOINT}/hero/${id}`,
      dto,
      file,
      "file"
    );
  },

  deleteHero: async (id: number): Promise<void> => {
    return await ApiService.delete<void>(`${ENDPOINT}/hero/${id}`);
  },

  // SERVICES (JSON Padrão - Sem alterações necessárias)
  createService: async (data: ServiceFormValues): Promise<ServiceData> => {
    return await ApiService.post<ServiceData>(`${ENDPOINT}/services`, data);
  },

  updateService: async (id: number, data: ServiceFormValues): Promise<void> => {
    return await ApiService.put<void>(`${ENDPOINT}/services/${id}`, data);
  },

  deleteService: async (id: number): Promise<void> => {
    return await ApiService.delete<void>(`${ENDPOINT}/services/${id}`);
  },
};
