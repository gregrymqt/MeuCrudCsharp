// features/auth/types/auth.dtos.ts
import type { UserSession } from './auth.types';

// O que recebemos de volta da API de Login
export interface LoginResponse {
  user: UserSession;
  token: string;
  refreshToken: string;
  expiration: string;
}

