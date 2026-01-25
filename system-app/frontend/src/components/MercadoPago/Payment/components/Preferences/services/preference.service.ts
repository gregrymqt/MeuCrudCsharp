import { ApiService } from "../../../../../../shared/services/api.service";

//
export const PreferenceService = {
  createPreference: async (amount: number, title: string): Promise<string> => {
    // Agora enviamos no corpo
    const payload = { 
        amount: amount,
        title: title, 
        description: "Acesso ao curso completo" 
    };
    
    const response = await ApiService.post<{ preferenceId: string }>(
      '/preferences', 
      payload
    );
    return response.preferenceId;
  }
};