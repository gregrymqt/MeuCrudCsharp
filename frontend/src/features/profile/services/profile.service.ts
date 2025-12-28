import { ApiService } from "../../../shared/services/api.service";
export const ProfileService = {
  // Endpoint: PUT /profile/avatar
  updateAvatar: async (
    formData: FormData
  ): Promise<{ avatarUrl: string; message: string }> => {
    // Usa o putFormData que já lida com headers multipart
    return await ApiService.putFormData<{ avatarUrl: string; message: string }>(
      "/profile/avatar",
      formData
    );
  },
};
