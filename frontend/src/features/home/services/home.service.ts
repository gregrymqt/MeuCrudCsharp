import { ApiService } from "../../../shared/services/api.service"; // Ajuste o caminho conforme seu projeto
import type {
  HomeContent,
  HeroSlideData,
  ServiceData,
  HeroFormValues,
  ServiceFormValues,
} from "../types/home.types";

const ENDPOINT = "/Home"; 

// --- HELPER: Converte Objeto JS -> FormData (Necessário para C# [FromForm]) ---
const createHeroFormData = (data: HeroFormValues): FormData => {
  const formData = new FormData();

  // Mapeamento dos campos de texto
  // O ASP.NET Core faz o binding case-insensitive (title -> Title)
  if (data.title) formData.append("title", data.title);
  if (data.subtitle) formData.append("subtitle", data.subtitle);
  if (data.actionText) formData.append("actionText", data.actionText);
  if (data.actionUrl) formData.append("actionUrl", data.actionUrl);

  // Mapeamento do Arquivo
  // Importante: "file" deve bater com "public IFormFile? File" no seu DTO C#
  if (data.newImage && data.newImage.length > 0) {
    formData.append("file", data.newImage[0]);
  }

  return formData;
};

export const HomeService = {

  // =========================================================
  // LEITURA (GET)
  // =========================================================
  getHomeContent: async (): Promise<HomeContent> => {
    return await ApiService.get<HomeContent>(`${ENDPOINT}`);
  },

  // =========================================================
  // HERO (Possui Upload - Usa FormData)
  // =========================================================
  createHero: async (data: HeroFormValues): Promise<HeroSlideData> => {
    const formData = createHeroFormData(data);
    // Usa postFormData para garantir que o browser defina o Boundary correto
    return await ApiService.postFormData<HeroSlideData>(
      `${ENDPOINT}/hero`,
      formData
    );
  },

  updateHero: async (id: number, data: HeroFormValues): Promise<void> => {
    const formData = createHeroFormData(data);
    return await ApiService.putFormData<void>(
      `${ENDPOINT}/hero/${id}`,
      formData
    );
  },

  deleteHero: async (id: number): Promise<void> => {
    return await ApiService.delete<void>(`${ENDPOINT}/hero/${id}`);
  },

  // =========================================================
  // SERVICES (Apenas Texto - Usa JSON padrão)
  // =========================================================
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