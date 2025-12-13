// features/auth/hooks/useAuth.ts
import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { ApiError, ApiService } from '../../../shared/services/api.service';
import { StorageService, STORAGE_KEYS } from '../../../shared/services/storage.service';
import type { UserSession } from '../types/auth.types';


// Nome do evento global para sincronizar abas e componentes
const AUTH_EVENT_NAME = 'greg:auth_update';

export const useAuth = () => {
  const navigate = useNavigate();

  // 1. ESTADO INICIAL (GET - Leitura Síncrona)
  // Inicializamos lendo do Storage para evitar "piscar" a tela de login se o user já estiver logado
  const [user, setUser] = useState<UserSession | null>(() => 
    StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION)
  );

  const isAuthenticated = !!user;

  // 2. OUVINTE DE EVENTOS (Reatividade Global)
  useEffect(() => {
    const handleAuthChange = () => {
      // Sempre que o evento disparar, relemos o storage e atualizamos o estado
      const updatedUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
      setUser(updatedUser);
    };

    // Escuta eventos da própria aba
    window.addEventListener(AUTH_EVENT_NAME, handleAuthChange);
    // Escuta eventos de OUTRAS abas (ex: logout em outra aba)
    window.addEventListener('storage', handleAuthChange);

    return () => {
      window.removeEventListener(AUTH_EVENT_NAME, handleAuthChange);
      window.removeEventListener('storage', handleAuthChange);
    };
  }, []);

  /**
   * DELETE: Logout (Invalida no Back e Limpa Front)
   */
  const logout = useCallback(async () => {
    try {
      // 1. Tenta avisar o backend para invalidar o token (Blacklist)
      // Não enviamos body, o token vai no Header automaticamente pelo ApiService
      await ApiService.post('/auth/logout', {});
    } catch (error) {
      // Se der erro (ex: servidor fora do ar), apenas logamos, 
      // mas CONTINUAMOS o logout local para não prender o usuário.
      console.error("Erro ao invalidar token no servidor:", error);
    } finally {
      // 2. Limpa Storage Local (Sempre executa, mesmo com erro no fetch)
      StorageService.clear();
      
      // 3. Dispara Evento Global
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
      
      // 4. Redireciona
      navigate('/login');
    }
  }, [navigate]);

  /**
   * UPDATE: Atualiza dados do usuário (Sync Local + API)
   * Útil para "Minha Conta" ou quando o usuário troca foto
   */
  const updateUser = async (data: UserSession) => {

      // 2. Mescla com os dados atuais da sessão (tokens, roles) para não perder nada
      const currentUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
      
      if (currentUser) {
        const newSession = { ...currentUser, ...data };
        
        // 3. Atualiza Storage e Dispara Evento
        StorageService.setItem(STORAGE_KEYS.USER_SESSION, newSession);
        window.dispatchEvent(new Event(AUTH_EVENT_NAME));
        
        return newSession;
      }
  };

  /**
   * GET (Refresh): Força uma busca dos dados mais recentes no servidor
   * Útil para chamar quando a página carrega, para garantir que o cache não está velho
   */
  const refreshSession = async () => {
    try {
      const freshUser = await ApiService.get<UserSession>('/auth/me');
      
      // Mantém o token atual, atualiza apenas dados do usuário
      const currentUser = StorageService.getItem<UserSession>(STORAGE_KEYS.USER_SESSION);
      const newSession = { ...currentUser, ...freshUser }; // Garante merges seguros

      StorageService.setItem(STORAGE_KEYS.USER_SESSION, newSession);
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));
    } catch (error: unknown) {
        if(error instanceof ApiError){
      // Se der 401 (token expirou), faz logout
      if (error.status === 401) {
        logout();
      }
    }
    }
  };

  /**
   * INICIA O FLUXO DO GOOGLE
   * Apenas redireciona o navegador para o endpoint do seu backend C#
   */
  const loginGoogle = () => {
    // Pegamos a URL base do Vite env ou hardcoded
    const baseUrl = import.meta.env.VITE_GENERAL__BASEURL || 'https://localhost:5045';
    // Redireciona o usuário para fora do React, direto para o controller do C#
    window.location.href = `${baseUrl}/api/auth/google-login`; 
  };

  /**
   * FINALIZA O FLUXO DO GOOGLE
   * Chamado quando o usuário volta do Google com o token na URL
   */
  const handleGoogleCallback = useCallback(async (token: string) => {
    try {
      // 1. Salva o token que veio na URL
      StorageService.setItem(STORAGE_KEYS.TOKEN, token);

      // 2. Busca os dados do usuário usando esse token novo
      const user = await ApiService.get<UserSession>('/auth/me');

      // 3. Salva a sessão e avisa o app
      StorageService.setItem(STORAGE_KEYS.USER_SESSION, user);
      window.dispatchEvent(new Event(AUTH_EVENT_NAME));

      // 4. Vai para a Home
      navigate('/', { replace: true });
    } catch (error) {
      console.error("Erro ao processar login Google", error);
      navigate('/login?error=google_failed');
    }
  }, [navigate]);

  return {
    user,
    isAuthenticated,
    logout,
    updateUser,
    refreshSession,
    loginGoogle,
    handleGoogleCallback
  };
};