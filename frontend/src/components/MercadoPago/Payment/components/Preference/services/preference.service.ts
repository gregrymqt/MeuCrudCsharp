// src/pages/Payment/services/preference.service.ts
import { ApiService } from "../../../../../../shared/services/api.service";

interface PreferenceResponse {
  preferenceId: string;
}

export const PreferenceService = {
  createPreference: async (amount: number): Promise<string> => {
    // Chamada: POST /api/preferences?amount=100.00
    // Ajuste conforme seu ApiService (se ele aceita query params no 3º argumento ou concatenado)
    const response = await ApiService.post<PreferenceResponse>(
      `/preferences?amount=${amount}`, 
      {} // Body vazio, pois o amount está na URL
    );
    return response.preferenceId;
  }
};