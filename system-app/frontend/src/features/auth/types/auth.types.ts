// Define o formato exato que vem do Backend (C#)
// UserSessionDto [cite: 1]
export interface UserSessionDto {
  publicId: string; // Mapeado de Guid [cite: 1]
  name: string;
  email: string;
  avatarUrl: string;
  roles: string[];
  hasActiveSubscription: boolean; // 
  hasPaymentHistory: boolean;     // [cite: 5]
}

// Interface usada no Front (pode incluir token se você mesclar no login)
export interface UserSession extends UserSessionDto {
  token?: string; // Mantemos opcional pois o token pode estar apenas no Storage separado
}