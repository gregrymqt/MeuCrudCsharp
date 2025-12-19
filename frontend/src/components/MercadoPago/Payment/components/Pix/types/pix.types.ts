// src/pages/Payment/components/Pix/pix.types.ts

export interface IdentificationType {
  id: string;
  name: string;
}

export interface PixPayer {
  firstName: string;
  lastName: string;
  email: string;
  identificationType: string;
  identificationNumber: string;
}

// O que enviamos para o Backend
export interface CreatePixDTO {
  transactionAmount: number;
  description: string;
  payer: {
    firstName: string;
    lastName: string;
    email: string;
    identification: {
      type: string;
      number: string;
    };
  };
}

// O que o Backend retorna (Baseado no seu renderPaymentView)
export interface PixResponse {
  qrCode: string;       // O código "copia e cola"
  qrCodeBase64: string; // A imagem em base64
  paymentId: string;    // ID para monitorar via SignalR
  expirationDate?: string;
}

// Estados da UI do PIX
export type PixStep = 'FORM' | 'QR_CODE' | 'APPROVED' | 'REJECTED';