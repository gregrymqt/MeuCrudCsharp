import type { UserSession } from '../../auth/types/auth.types';
import type { SubscriptionDetails } from '../types/profile.types'; // Seu tipo da UI [cite: 22]

export const ProfileMapper = {
  /**
   * Transforma o objeto complexo do UserSession (Auth) 
   * no objeto simples esperado pelos componentes de Assinatura.
   */
  toSubscriptionDetails: (user: UserSession | null): SubscriptionDetails | null => {
    // Se não tiver user ou subscription, retorna null para a UI tratar
    if (!user?.subscription) return null;

    return {
      id: String(user.subscription.id), // Garante string
      planName: user.subscription.plan?.name || 'Plano Desconhecido', // [cite: 9]
      status: user.subscription.status || 'unknown', // [cite: 10]
      amount: user.subscription.amount || 0, // [cite: 11]
      lastFourCardDigits: user.subscription.lastFourCardDigits || '****',
      nextBillingDate: user.subscription.currentPeriodEndDate // [cite: 11]
    };
  }
};