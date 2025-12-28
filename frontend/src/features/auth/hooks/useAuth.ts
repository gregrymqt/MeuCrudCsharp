import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { StorageService, STORAGE_KEYS } from '../../../shared/services/storage.service';
import { ApiError } from '../../../shared/services/api.service';
import { authService } from '../services/auth.service';
import type { UserSession } from '../types/auth.types';

const AUTH_EVENT_NAME = 'greg:auth_update';

export const useAuth = () => {
  const navigate = useNavigate();

  // 1. ESTADO INICIAL
  const [user, setUser] = useState<UserSession | null>(() => 
    StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION)
  );

  const isAuthenticated = !!user;

  // 2. REATIVIDADE (Sincroniza abas)
  useEffect(() => {
    const handleAuthChange = () => {
      const updatedUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
      setUser(updatedUser);
    };

    window.addEventListener(AUTH_EVENT_NAME, handleAuthChange);
    window.addEventListener('storage', handleAuthChange);

    return () => {
      window.removeEventListener(AUTH_EVENT_NAME, handleAuthChange);
      window.removeEventListener('storage', handleAuthChange);
    };
  }, []);

  /**
   * LOGIN GOOGLE
   */
  const handleGoogleCallback = useCallback(async (token: string) => {
    try {
      // 1. Salva Token
      StorageService.setItem(STORAGE_KEYS.TOKEN, token);

      // 2. Busca dados otimizados (Agora vem rápido e leve)
      const userDto = await authService.getMe();

      // 3. Monta sessão (Junta DTO + Token se quiser persistir junto)
      const sessionData: UserSession = {
        ...userDto,
        token // Opcional, já que salvamos na chave TOKEN acima
      };

      // 4. Salva no Storage
      StorageService.setItem(STORAGE_KEYS.USER_SESSION, sessionData);
      
      // 5. Notifica app
      window.dispatchEvent(new Event(AUTH_EVENT_NAME)); 
      navigate('/', { replace: true });

    } catch (error) {
      console.error("Erro no login Google:", error);
      navigate('/login?error=google_failed');
    }
  }, [navigate]);

  /**
   * LOGOUT
   */
  const logout = useCallback(async () => {
    try {
      await authService.logout();
    } catch (error) {
      console.error("Erro no logout:", error);
      // Ignora erro de rede no logout
    } finally {
      StorageService.clear();
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
      navigate('/login');
    }
  }, [navigate]);

  /**
   * REFRESH SESSION
   * Útil para atualizar as flags (ex: acabou de assinar, hasActiveSubscription vira true)
   */
  const refreshSession = async () => {
    try {
      const freshData = await authService.getMe();
      const currentUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
      
      // Atualiza mantendo o token existente se ele não vier do getMe
      const newSession: UserSession = {
        ...currentUser, // Mantém token antigo
        ...freshData,   // Sobrescreve dados e flags [cite: 17]
      };

      StorageService.setItem(STORAGE_KEYS.USER_SESSION, newSession);
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
    } catch (error: unknown) {
      if (error instanceof ApiError && error.status === 401) {
        logout();
      }
    }
  };

  return {
    user,
    isAuthenticated,
    logout,
    refreshSession,
    loginGoogle: authService.loginGoogle,
    handleGoogleCallback
  };
};