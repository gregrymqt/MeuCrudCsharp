// src/components/Payment/MercadoPagoBrick.tsx
import React, { useEffect } from 'react';
import { initMercadoPago, Payment } from '@mercadopago/sdk-react';
import { BrickPaymentData, CreditCardConfig } from '../../pages/Payment/types/credit-card.types';

// Inicialize com sua PUBLIC KEY (Idealmente vinda de variáveis de ambiente)
// Nota: Se já inicializou no App.tsx, não precisa chamar de novo, mas não faz mal garantir.
initMercadoPago(process.env.REACT_APP_MP_PUBLIC_KEY || 'SUA_PUBLIC_KEY_AQUI', { locale: 'pt-BR' });

interface MercadoPagoBrickProps {
  config: CreditCardConfig;
  onSubmit: (param: BrickPaymentData) => Promise<void>;
  onError?: (error: any) => void;
}

export const MercadoPagoBrick: React.FC<MercadoPagoBrickProps> = ({ config, onSubmit, onError }) => {

  // Configuração da Inicialização do Brick baseada no modo
  const initialization = {
    amount: config.amount,
    preferenceId: config.preferenceId,
  };

  const customization = {
    paymentMethods: {
      creditCard: 'all' as const,
      // Se for assinatura (parcelado fixo), podemos forçar maxInstallments ou deixar o usuário escolher
      maxInstallments: config.mode === 'subscription' ? 1 : 12, 
    },
    visual: {
      style: {
        theme: 'bootstrap' as const, // Mantendo o tema do seu legado
        customVariables: {
          formBackgroundColor: '#ffffff',
          baseColor: '#007bff', // Cor primária do seu sistema
        }
      },
    },
  };

  return (
    <div className="mp-brick-container">
      <Payment
        initialization={initialization}
        customization={customization}
        onSubmit={async (param) => {
          // O SDK retorna todos os dados do cartão tokenizados aqui
          console.log('Dados do Brick:', param);
          // Repassamos para o componente pai tratar (enviar ao backend)
          await onSubmit(param as unknown as BrickPaymentData);
        }}
        onError={(error) => {
          console.error('Erro no Brick MP:', error);
          if (onError) onError(error);
        }}
      />
    </div>
  );
};