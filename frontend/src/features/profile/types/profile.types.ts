// features/profile/types/profile.types.ts

// 1. Importamos os modelos globais que são a "Fonte da Verdade"
import type { User, Payment, Subscription } from '../../../types/models';

// ==========================================
// VIEW MODELS / DTOS (Dados para a Tela)
// ==========================================

/**
 * UserProfile
 * Reutiliza os campos existentes na interface global User.
 * Usamos 'Pick' para selecionar apenas o que a tela de perfil precisa.
 * Ref: [cite: 4, 17]
 */
export type UserProfile = Pick<User, 'name' | 'email' | 'avatarUrl'>;

/**
 * SubscriptionDetails
 * Representa um resumo da assinatura para exibição.
 * Nota: O status aqui é mais estrito ('active' | 'paused'...) do que o string genérico do banco.
 * Ref: [cite: 1, 21]
 */

export type SubscriptionDetails = Pick<Subscription, 'id' | 'amount' | 'status' | 'lastFourCardDigits' | 'nextBillingDate'> & {
  planName: string;
};

/**
 * PaymentItem
 * Baseado na entidade Payment global, mas simplificado para a lista de histórico.
 * Estende os campos básicos e adiciona 'description' que é específico dessa view.
 * Ref: [cite: 6, 22]
 */
export interface PaymentItem extends Pick<Payment, 'id' | 'amount' | 'createdAt'> {
  status: string; // Ou mapear para um Enum se tiver PaymentStatus global
  description?: string;
}

/**
 * CreditCard
 * DTO específico de gateway (MercadoPago), geralmente não salvo como entidade completa no banco.
 * Mantemos a definição local pois não existe em models.ts.
 * Ref: [cite: 7]
 */
export interface CreditCard {
  id: string;
  lastFourDigits: string;
  brand: string;
  expirationMonth: number;
  expirationYear: number;
}

// ==========================================
// REQUEST DTOS (Dados enviados para a API)
// ==========================================

export interface UpdateStatusRequest {
  status: 'paused' | 'cancelled' | 'active'; 
}

export interface UpdateCardRequest {
  token: string;
}