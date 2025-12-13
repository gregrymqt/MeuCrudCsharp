// features/auth/types/auth.dtos.ts
import type { UserSession } from './auth.types';

// O que enviamos para Logar
export interface LoginDTO {
  email: string;
  password?: string;
  googleId?: string; // Caso use login social
}

// O que recebemos de volta da API de Login
export interface LoginResponse {
  user: UserSession;
  token: string;
  refreshToken: string;
  expiration: string;
}

// DTO para atualização parcial do usuário
export interface UpdateUserDTO {
  name?: string;
  avatarUrl?: string;
  phoneNumber?: string;
  currentPassword?: string;
  newPassword?: string;
}