// src/components/Payment/MercadoPagoBrick.tsx
import React from "react"; // [Correção 1]: Removido { useEffect } pois não era usado
import { initMercadoPago, Payment } from "@mercadopago/sdk-react";
import type {
  CreditCardConfig,
  BrickPaymentData,
} from "../types/credit-card.types";

// Inicialize com sua PUBLIC KEY
initMercadoPago(process.env.REACT_APP_MP_PUBLIC_KEY || "SUA_PUBLIC_KEY_AQUI", {
  locale: "pt-BR",
});

interface MercadoPagoBrickProps {
  config: CreditCardConfig;
  onSubmit: (param: BrickPaymentData) => Promise<void>;
  onError?: (error: unknown) => void;
}

export const MercadoPagoBrick: React.FC<MercadoPagoBrickProps> = ({
  config,
  onSubmit,
  onError,
}) => {
  // Configuração da Inicialização do Brick baseada no modo
  const initialization = {
    amount: config.amount ?? 0,
    preferenceId: config.preferenceId,
  };

  const customization = {
    paymentMethods: {
      creditCard: "all" as const,
      maxInstallments: config.mode === "subscription" ? 1 : 12,
    },
    visual: {
      style: {
        theme: "bootstrap" as const,
        customVariables: {
          formBackgroundColor: "#ffffff",
          baseColor: "#007bff",
        },
      },
      Texts: {
        formTitle: "Dados do Cartão",
        submit: "Salvar Cartão",
      },
    },
  };

  return (
    <div className="mp-brick-container">
      <Payment
        initialization={initialization}
        customization={customization}
        onSubmit={async (param) => {
          console.log("Dados do Brick:", param);
          await onSubmit(param as unknown as BrickPaymentData);
        }}
        onError={(error) => {
          console.error("Erro no Brick MP:", error);
          if (onError) onError(error);
        }}
      />
    </div>
  );
};
