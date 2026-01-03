import { ApiService } from "../../../shared/services/api.service";

// Definição do retorno esperado
interface AvatarResponse {
  avatarUrl: string;
  message: string;
}

export const ProfileService = {
  // Endpoint: PUT /profile/avatar
  updateAvatar: async (file: File): Promise<AvatarResponse> => {
    // Passamos um objeto vazio {} como corpo, pois só queremos enviar o arquivo
    return await ApiService.putWithFile<AvatarResponse, Record<string, unknown>>(
      "/profile/avatar",
      {}, 
      file,
      'file' // Nome do campo esperado no C# (IFormFile file)
    );
  },
};