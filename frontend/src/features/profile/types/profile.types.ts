import type { User, Payment, Subscription } from '../../../types/models';

export type UserProfile = Pick<User, 'name' | 'email' | 'avatarUrl'>;

export type SubscriptionDetails = Pick<Subscription, 'id' | 'amount' | 'status' | 'lastFourCardDigits' | 'nextBillingDate'> & {
  planName: string;
};
export interface PaymentItem extends Pick<Payment, 'id' | 'amount' | 'createdAt'> {
  status: string; // Ou mapear para um Enum se tiver PaymentStatus global
  description?: string;
}

export interface CreditCard {
  id: string;
  lastFourDigits: string;
  brand: string;
  expirationMonth: number;
  expirationYear: number;
}

export interface UpdateStatusRequest {
  status: 'paused' | 'cancelled' | 'active'; 
}

export interface UpdateCardRequest {
  token: string;
}