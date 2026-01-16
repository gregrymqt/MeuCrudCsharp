import  { ApiService } from "../../../../shared/services/api.service";
import type { PaymentItems } from "../types/transactions.type";

export const TransactionService = {
  getPaymentHistory: async (): Promise<PaymentItems[]> => {
    // Rota ajustada para bater com a Controller [HttpGet("history")] em "api/payments"
    return await ApiService.get<PaymentItems[]>('/payments/history');
  },

  requestRefund: async (paymentId: string): Promise<{ message: string }> => {
    return await ApiService.post(`/refunds/${paymentId}`, {}); 
  }
};