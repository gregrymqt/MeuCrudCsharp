import  { ApiService } from '../../../shared/services/api.service';
import type { 
  CreditCard, 
  UpdateStatusRequest, 
  UpdateCardRequest 
} from '../types/profile.types';

export const ProfileService = {

  // Endpoint: GET /cards 
  getCards: async (): Promise<CreditCard[]> => {
    return await ApiService.get<CreditCard[]>('/profile/cards');
  },

  // Endpoint: DELETE /cards/{cardId} 
  deleteCard: async (cardId: string): Promise<void> => {
    await ApiService.delete(`/profile/cards/${cardId}`);
  },

  // Endpoint: PUT /subscription/status 
  updateSubscriptionStatus: async (data: UpdateStatusRequest): Promise<{ message: string }> => {
    return await ApiService.put('/profile/subscription/status', data);
  },

  // Endpoint: PUT /subscription/card 
  updateSubscriptionCard: async (data: UpdateCardRequest): Promise<{ message: string }> => {
    return await ApiService.put('/profile/subscription/card', data);
  },

  // Endpoint: PUT /profile/avatar 
  updateAvatar: async (formData: FormData): Promise<{ avatarUrl: string; message: string }> => {
    // Usa o putFormData que já lida com headers multipart
    return await ApiService.putFormData<{ avatarUrl: string; message: string }>(
      '/profile/avatar', 
      formData
    );
  }
};