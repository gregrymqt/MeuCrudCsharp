import { ApiService } from "../../../shared/services/api.service";

interface AvatarResponse {
  avatarUrl: string;
  message: string;
}

export const ProfileService = {
  // Endpoint: POST /api/user-account/avatar
  updateAvatar: async (file: File): Promise<AvatarResponse> => {
    return await ApiService.postWithFile<
      AvatarResponse,
      Record<string, unknown>
    >(
      "/user-account/avatar",
      {}, // Nenhum dado extra, só o arquivo
      file,
      "file" // Nome da chave que o C# espera ([FromForm] IFormFile file)
    );
  },
};
