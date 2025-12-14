// src/components/Shared/Payment/MercadoPagoBrick.tsx
import React from 'react';
import { initMercadoPago, CardPayment } from '@mercadopago/sdk-react';

// Inicialize sua Public Key aqui (ou pegue de .env)
initMercadoPago('SUA_PUBLIC_KEY_AQUI', { locale: 'pt-BR' });

interface MercadoPagoBrickProps {
  amount?: number; // Valor para tokenização (geralmente R$ 1.00 para update)
  onSubmit: (token: string) => Promise<void>;
}

export const MercadoPagoBrick: React.FC<MercadoPagoBrickProps> = ({ amount = 1, onSubmit }) => {
  
  return (
    <div className="mp-brick-wrapper">
      <CardPayment
        initialization={{ amount: amount }}
        customization={{
          visual: {
            style: {
              theme: 'bootstrap', // Mantendo o tema que você usava [cite: 58]
            },
          },
        }}
        onSubmit={async (param) => {
          // O SDK do React retorna o token dentro do objeto param
          if (param && param.token) {
            await onSubmit(param.token);
          }
        }}
      />
    </div>
  );
};