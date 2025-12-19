import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { StorageService, STORAGE_KEYS } from '../../../shared/services/storage.service';
import { ApiError } from '../../../shared/services/api.service';
import { authService } from '../services/auth.service'; // Importa o novo service
import type { UserSession } from '../types/auth.types';

const AUTH_EVENT_NAME = 'greg:auth_update';

export const useAuth = () => {
  const navigate = useNavigate();

  // 1. ESTADO INICIAL (Cache Local)
  const [user, setUser] = useState<UserSession | null>(() => 
    StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION) 
  );

  const isAuthenticated = !!user;

  // 2. REATIVIDADE (Sincroniza abas e eventos de login/logout)
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
      // 1. Salva o Token recebido na URL
      StorageService.setItem(STORAGE_KEYS.TOKEN, token);

      // 2. Busca os dados completos no Backend (User + Subscription + Payments)
      const fullUserData = await authService.getMe();

      // 3. CACHE: Salva TUDO no Storage (Preenche o "cookie" do front)
      StorageService.setItem(STORAGE_KEYS.USER_SESSION, fullUserData); 
      
      // 4. Avisa a aplicação que o usuário mudou
      window.dispatchEvent(new Event(AUTH_EVENT_NAME)); 

      navigate('/', { replace: true });
    } catch (error) {
      console.error("Erro ao processar login Google", error);
      navigate('/login?error=google_failed');
    }
  }, [navigate]);

  /**
   * LOGOUT
   */
  const logout = useCallback(async () => {
    try {
      // Tenta invalidar no backend (Blacklist)
      await authService.logout(); 
    } catch (error) {
      console.error("Erro logout server (ignorando para limpar local):", error); 
    } finally {
      // Sempre limpa o front, mesmo se o server falhar
      StorageService.clear(); 
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
      navigate('/login');
    }
  }, [navigate]);

  /**
   * REFRESH SESSION (Ex: Ao recarregar a página)
   */
  const refreshSession = async () => {
    try {
      const freshUser = await authService.getMe(); 
      
      const currentUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
      // Mescla segura para não perder campos locais se houver
      const newSession = { ...currentUser, ...freshUser }; 

      StorageService.setItem(STORAGE_KEYS.USER_SESSION, newSession); 
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
    } catch (error: unknown) {
      if (error instanceof ApiError && error.status === 401) {
        logout(); 
      }
    }
  };

  /**
   * UPDATE USER (Atualização Otimista/Parcial)
   */
  const updateUser = async (data: Partial<UserSession>) => { // Ajustado para Partial
    const currentUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
    if (currentUser) {
      const newSession = { ...currentUser, ...data } as UserSession;
      StorageService.setItem(STORAGE_KEYS.USER_SESSION, newSession);
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
      return newSession;
    }
  };

  return {
    user,
    isAuthenticated,
    logout,
    updateUser,
    refreshSession,
    loginGoogle: authService.loginGoogle, // Passa direto do service [cite: 19]
    handleGoogleCallback
  };
};