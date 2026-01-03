import { ApiService } from "../../../shared/services/api.service";
import type {
  AboutSectionFormValues,
  AboutSectionData,
  TeamMemberFormValues,
  TeamMember,
  AboutPageResponse,
} from "../types/about.types";

const ENDPOINT = "/About";

export const AboutService = {
  // LEITURA
  getAboutPage: async (): Promise<AboutPageResponse> => {
    return await ApiService.get<AboutPageResponse>(`${ENDPOINT}`);
  },

  // --- SEÇÃO (Texto + Imagem) ---

  createSection: async (
    data: AboutSectionFormValues
  ): Promise<AboutSectionData> => {
    const { newImage, ...dto } = data;
    const file = newImage && newImage.length > 0 ? newImage[0] : null;

    return await ApiService.postWithFile<AboutSectionData, typeof dto>(
      `${ENDPOINT}/sections`,
      dto,
      file,
      "file"
    );
  },

  updateSection: async (
    id: number,
    data: AboutSectionFormValues
  ): Promise<void> => {
    const { newImage, ...dto } = data;
    const file = newImage && newImage.length > 0 ? newImage[0] : null;

    return await ApiService.putWithFile<void, typeof dto>(
      `${ENDPOINT}/sections/${id}`,
      dto,
      file,
      "file"
    );
  },

  deleteSection: async (id: number): Promise<void> => {
    return await ApiService.delete<void>(`${ENDPOINT}/sections/${id}`);
  },

  // --- MEMBROS DA EQUIPE (Foto) ---

  createTeamMember: async (data: TeamMemberFormValues): Promise<TeamMember> => {
    const { newPhoto, ...dto } = data;
    const file = newPhoto && newPhoto.length > 0 ? newPhoto[0] : null;

    // Se o backend espera 'newPhoto', altere o 4º parâmetro para 'newPhoto'
    return await ApiService.postWithFile<TeamMember, typeof dto>(
      `${ENDPOINT}/team`,
      dto,
      file,
      "file"
    );
  },

  updateTeamMember: async (
    id: number | string,
    data: TeamMemberFormValues
  ): Promise<void> => {
    const { newPhoto, ...dto } = data;
    const file = newPhoto && newPhoto.length > 0 ? newPhoto[0] : null;

    return await ApiService.putWithFile<void, typeof dto>(
      `${ENDPOINT}/team/${id}`,
      dto,
      file,
      "file"
    );
  },

  deleteTeamMember: async (id: number | string): Promise<void> => {
    return await ApiService.delete<void>(`${ENDPOINT}/team/${id}`);
  },
};
