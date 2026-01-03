import { ApiService } from "../../../shared/services/api.service";
import type { 
  AboutSectionFormValues, 
  AboutSectionData, 
  TeamMemberFormValues, 
  TeamMember,
  AboutPageResponse 
} from "../types/about.types";

const ENDPOINT = "/About"; // Base do controller

// --- HELPER PARA CONVERTER OBJETO EM FORMDATA ---
// Correção: Removemos o Record<string, T> e usamos apenas T.
// O 'keyof T' garante que a chave do arquivo existe no tipo.
const createFormData = <T extends object>(
  data: T,
  fileKey: keyof T,
  fileList?: FileList
): FormData => {
  const formData = new FormData();

  // 'as any' é necessário aqui para iterar sobre chaves desconhecidas pelo compilador
  // mas seguro pois estamos apenas convertendo para string
  const dataObj = data as Record<string, any>;

  Object.keys(dataObj).forEach((key) => {
    // Ignora a chave do arquivo (tratada depois) e valores nulos
    if (key !== fileKey && dataObj[key] !== undefined && dataObj[key] !== null) {
      formData.append(key, dataObj[key].toString());
    }
  });

  // Anexa o arquivo físico se existir
  if (fileList && fileList.length > 0) {
    // Atenção: O backend C# deve esperar 'file' no [FromForm] ou propriedade IFormFile
    formData.append("file", fileList[0]);
  }

  return formData;
};

export const AboutService = {
  // =========================================================
  // LEITURA (PÚBLICA)
  // =========================================================

  // Retorna todo o conteúdo da página tipado corretamente
  getAboutPage: async (): Promise<AboutPageResponse> => {
    // GET /api/About
    return await ApiService.get<AboutPageResponse>(`${ENDPOINT}`);
  },

  // =========================================================
  // SEÇÃO 1: TEXTO + IMAGEM
  // =========================================================

  createSection: async (
    data: AboutSectionFormValues
  ): Promise<AboutSectionData> => {
    // Converte para FormData para enviar a imagem
    // O erro de Record<string, T> foi corrigido na função helper
    const formData = createFormData<AboutSectionFormValues>(data, "newImage", data.newImage);

    // POST /api/About/sections
    return await ApiService.postFormData<AboutSectionData>(
      `${ENDPOINT}/sections`,
      formData
    );
  },

  updateSection: async (
    id: number,
    data: AboutSectionFormValues
  ): Promise<void> => {
    const formData = createFormData<AboutSectionFormValues>(data, "newImage", data.newImage);

    // PUT /api/About/sections/{id}
    return await ApiService.putFormData<void>(
      `${ENDPOINT}/sections/${id}`,
      formData
    );
  },

  deleteSection: async (id: number): Promise<void> => {
    // DELETE /api/About/sections/{id}
    return await ApiService.delete<void>(`${ENDPOINT}/sections/${id}`);
  },

  // =========================================================
  // SEÇÃO 2: MEMBROS DA EQUIPE
  // =========================================================

  createTeamMember: async (data: TeamMemberFormValues): Promise<TeamMember> => {
    const formData = createFormData<TeamMemberFormValues>(data, "newPhoto", data.newPhoto);

    // POST /api/About/team
    return await ApiService.postFormData<TeamMember>(
      `${ENDPOINT}/team`,
      formData
    );
  },

  updateTeamMember: async (
    id: number | string,
    data: TeamMemberFormValues
  ): Promise<void> => {
    const formData = createFormData<TeamMemberFormValues>(data, "newPhoto", data.newPhoto);

    // PUT /api/About/team/{id}
    return await ApiService.putFormData<void>(
      `${ENDPOINT}/team/${id}`,
      formData
    );
  },

  deleteTeamMember: async (id: number | string): Promise<void> => {
    // DELETE /api/About/team/{id}
    return await ApiService.delete<void>(`${ENDPOINT}/team/${id}`);
  },
};