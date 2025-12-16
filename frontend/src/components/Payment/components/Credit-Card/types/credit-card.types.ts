// src/pages/Payment/types/credit-card.types.ts

export type CreditCardMode = 'subscription' | 'payment'; // Assinatura vs Pagamento Único

export interface CreditCardConfig {
  mode: CreditCardMode;
  preferenceId?: string; // Obrigatório para Pagamento Único (Checkout Pro)
  amount?: number;       // Opcional, caso não use preferenceId
}

// O dado que o Brick devolve quando o usuário clica em "Pagar"
export interface BrickPaymentData {
  token: string;
  issuer_id: string;
  payment_method_id: string;
  transaction_amount: number;
  installments: number;
  payer: {
    email: string;
    identification: {
      type: string;
      number: string;
    };
  };
}

// Props do Componente Wrapper
export interface CreditCardPaymentProps {
  planName: string;
  amount: number;
  // Se for parcelado, geralmente é via assinatura (subscription)
  // Se for à vista, é via pagamento único (payment)
  mode: CreditCardMode; 
  userEmail?: string;
}

// DTO que o Backend C# espera no Body
export interface CreditCardPaymentRequestDto {
  token: string;
  installments: number;
  paymentMethodId: string;
  issuerId: string;
  amount: number;
  plano?: string; // "anual" aciona a lógica de assinatura no backend
  planExternalId: string; // Guid
  payer: {
    email: string;
    firstName?: string;
    lastName?: string;
    identification: {
      type: string;
      number: string;
    };
  };
}

// Resposta unificada do Backend
export interface PaymentResponse {
  status: string;
  id?: string;
  message?: string;
}