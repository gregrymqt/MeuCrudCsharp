// src/pages/Payment/services/credit-card.service.ts
import { ApiService } from "../../../../../../shared/services/api.service"; // Ajuste o caminho
import type { CreditCardPaymentRequestDto, PaymentResponse } from "../types/credit-card.types";

export const CreditCardService = {
  processPayment: async (data: CreditCardPaymentRequestDto): Promise<PaymentResponse> => {
    // 1. Gerar UUID v4 para Idempotência (Obrigatório pelo Controller C#)
    const idempotencyKey = self.crypto.randomUUID();

    // 2. Enviar para o endpoint configurado no Controller
    // O ApiService já concatena '/api', então chamamos '/credit/card/process-payment'
    return await ApiService.post<PaymentResponse>(
      '/credit/card/process-payment',
      data,
      {
        headers: {
          'X-Idempotency-Key': idempotencyKey
        }
      }
    );
  }
};