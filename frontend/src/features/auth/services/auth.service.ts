import { ApiService } from '../../../shared/services/api.service';
import type { UserSession } from '../types/auth.types';

export const authService = {
  /**
   * Redireciona para o endpoint do Backend que inicia o fluxo OAuth
   */
  loginGoogle: () => {
    // URL base do backend (C#)
    const baseUrl = import.meta.env.VITE_GENERAL__BASEURL || 'https://localhost:5045'; 
    window.location.href = `${baseUrl}/api/auth/google-login`; 
  },

  /**
   * Busca os dados completos do usuário (Perfil + Assinatura + Pagamentos)
   * Endpoint: GET /api/auth/me
   */
  getMe: async (): Promise<UserSession> => {
    return await ApiService.get<UserSession>('/auth/me'); 
  },

  /**
   * Invalida o token no backend (Blacklist)
   * Endpoint: POST /api/auth/logout
   */
  logout: async (): Promise<void> => {
    await ApiService.post('/auth/logout', {}); 
  }
};