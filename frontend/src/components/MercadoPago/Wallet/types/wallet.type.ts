/**
 * Representa o cartão retornado pela API (GET /api/v1/wallet)
 */
export interface WalletCard {
  id: string;
  lastFourDigits: string;
  expirationMonth: number;
  expirationYear: number;
  paymentMethodId: string; // ex: 'visa', 'master', 'amex'
  isSubscriptionActiveCard: boolean; // Flag crítica para a UI (Badge "Principal" e Bloqueio de Delete)
}

/**
 * Payload enviado para adicionar um cartão (POST /api/v1/wallet)
 */
export interface AddCardPayload {
  cardToken: string; // Token gerado pelo SDK do Mercado Pago no front (mp.js)
}