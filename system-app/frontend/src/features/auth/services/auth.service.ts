import { ApiService } from '../../../shared/services/api.service';
import type { UserSessionDto } from '../types/auth.types';

export const authService = {
  /**
   * Redireciona para o Google Login
   */
  loginGoogle: () => {
    const baseUrl = import.meta.env.VITE_GENERAL__BASEURL || 'https://localhost:5045';
    window.location.href = `${baseUrl}/api/auth/google-login`;
  },

  /**
   * Busca os dados LEVES do usuário (Perfil + Flags booleanas)
   * Endpoint: GET /api/auth/me
   * Retorna: UserSessionDto (sem listas pesadas)
   */
  getMe: async (): Promise<UserSessionDto> => {
    // O backend agora retorna apenas dados básicos e os booleanos [cite: 4, 5]
    return await ApiService.get<UserSessionDto>('/auth/me');
  },

  /**
   * Logout no servidor
   */
  logout: async (): Promise<void> => {
    await ApiService.post('/auth/logout', {});
  }
};