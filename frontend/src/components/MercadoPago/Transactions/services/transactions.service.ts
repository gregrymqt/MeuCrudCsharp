import  { ApiService } from "../../../../shared/services/api.service";
import type { PaymentItems } from "../types/transactions.type";

export const TransactionService = {
  getPaymentHistory: async (): Promise<PaymentItems[]> => {
    return await ApiService.get<PaymentItems[]>('/profile/payment-history');
  },

  requestRefund: async (paymentId: string): Promise<{ message: string }> => {
    return await ApiService.post(`/profile/refunds/${paymentId}`, {}); 
  }
};