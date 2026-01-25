import React, { useCallback } from 'react';

import styles from '../styles/AddCardForm.module.scss';

import { MercadoPagoBrick } from '../../Payment/components/Credit-Card/components/MercadoPagoBrick';
import type { BrickPaymentData } from '../../Payment/components/Credit-Card/types/credit-card.types';
import { usePreference } from '../../Payment/components/Preferences/hooks/usePreference';
import { useWallet } from '../hooks/useWallet';

interface AddCardFormProps {
  onSuccess?: () => void; // Callback para fechar modal ou feedback extra
  onCancel?: () => void;  // Callback para botão fechar
}

export const AddCardForm: React.FC<AddCardFormProps> = ({ onSuccess, onCancel }) => {
  const { addCard } = useWallet();
  
  // 1. Geramos uma preferência dummy apenas para carregar o Brick visualmente.
  // O valor 1.00 é simbólico para inicializar o form, mas não vamos processar esse pagamento no onSubmit.
  const { preferenceId, loading: prefLoading, error: prefError } = usePreference(1, true);

  // 2. Função disparada quando o usuário clica no botão de ação do Brick
  const handleBrickSubmit = useCallback(async (data: BrickPaymentData) => {
    // AQUI ESTÁ A MÁGICA:
    // O Brick devolve os dados, incluindo o 'token'.
    // Nós pegamos esse token e enviamos para nossa API de Wallet (Salvar Cartão)
    // ignorando a transação de pagamento do MP neste momento.
    
    if (!data.token) {
      console.error('Token não gerado pelo Brick');
      return;
    }

    const success = await addCard(data.token);

    if (success && onSuccess) {
      onSuccess();
    }
  }, [addCard, onSuccess]);

  // Se houver erro crítico na criação da preferência
  if (prefError) {
    return (
      <div className={styles['add-card-container']}>
        <div className={styles['error-feedback']}>
          <p>{prefError}</p>
          <button onClick={onCancel} className={styles['btn-retry']}>Tentar Novamente</button>
        </div>
      </div>
    );
  }

  return (
    <div className={styles['add-card-container']}>
      <div className={styles['form-header']}>
        <h3>Novo Cartão de Crédito</h3>
        {onCancel && (
          <button className={styles['btn-close']} onClick={onCancel} aria-label="Fechar">
            &times;
          </button>
        )}
      </div>

      {/* Estado de Loading enquanto busca o ID no backend */}
      {prefLoading && (
        <div className={styles['loading-state']}>
          <div className={styles['spinner']} />
          <p>Preparando conexão segura...</p>
        </div>
      )}

      {/* Renderização do Brick quando temos o ID */}
      {!prefLoading && preferenceId && (
        <div className={styles['brick-wrapper']}>
          <MercadoPagoBrick
            config={{
              mode: 'subscription', // Força layout simplificado se sua lógica interna tratar isso
              preferenceId: preferenceId,
              amount: 1 // Necessário para config visual, mas irrelevante para o salvamento
            }}
            onSubmit={handleBrickSubmit}
            onError={(error) => console.error("Erro renderização brick:", error)}
          />
        </div>
      )}
    </div>
  );
};