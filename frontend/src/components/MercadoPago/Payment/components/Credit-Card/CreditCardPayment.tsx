// src/pages/Payment/components/CreditCard/CreditCardPayment.tsx
import React from 'react';
import styles from './CreditCardPayment.module.scss';
import { MercadoPagoBrick } from './components/MercadoPagoBrick'; // Ajuste o caminho
import { useCreditCardPayment } from './hooks/useCreditCardPayment'; // Ajuste o caminho
import type { CreditCardPaymentProps } from './types/credit-card.types';

// Interface estendida
interface ExtendedProps extends CreditCardPaymentProps {
  planId: string;
  preferenceId: string; // <--- AGORA OBRIGATÓRIO
  onPaymentSuccess?: () => void;
}

export const CreditCardPayment: React.FC<ExtendedProps> = ({ 
  planName, 
  planId,
  amount, 
  mode,
  preferenceId, // Recebido do Pai
  onPaymentSuccess
}) => {
  
  const { loading, error, handleCreditCardSubmit } = useCreditCardPayment({
    planId,
    planName,
    amount,
    mode,
    onSuccess: () => {
      if (onPaymentSuccess) onPaymentSuccess();
    }
  });

  if (loading) {
    return (
      <div className={styles.loadingState}>
        <div className={styles.spinner}></div>
        <h3>Processando...</h3>
        <p>Estamos validando seu pagamento junto à operadora.</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      {/* Header e Erros (Mantidos iguais) */}
      <div className={styles.header}>
        <h3>
          <i className="fas fa-credit-card"></i> 
          {mode === 'subscription' ? ' Assinatura Recorrente' : ' Pagamento via Cartão'}
        </h3>
        <p>
          Plano selecionado: <strong>{planName}</strong> <br/>
          Valor: <strong>R$ {amount.toFixed(2)}</strong>
        </p>
      </div>

      {error && (
        <div className="alert alert-danger">
          <i className="fas fa-times-circle"></i> {error}
        </div>
      )}

      {/* Wrapper do Brick */}
      <div className={styles.brickWrapper}>
        <MercadoPagoBrick
          config={{
            mode: mode,
            amount: amount,
            preferenceId: preferenceId // <--- PASSADO AQUI
          }}
          onSubmit={handleCreditCardSubmit}
          onError={(e) => console.error("Erro brick", e)}
        />
      </div>

      <div className={styles.secureBadge}>
        <i className="fas fa-lock"></i>
        <span>Pagamento 100% seguro processado pelo Mercado Pago.</span>
      </div>
    </div>
  );
};