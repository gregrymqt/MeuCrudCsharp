import React from 'react';
import styles from '../../styles/UserSubscriptionActions.module.scss';
import  { Card } from '../../../../Card/Card';

interface SubscriptionActionsProps {
  status: string | null;
  onPause: () => void;
  onReactivate: () => void;
  onCancel: () => void;
  isProcessing: boolean;
}

export const SubscriptionActions: React.FC<SubscriptionActionsProps> = ({
  status,
  onPause,
  onReactivate,
  onCancel,
  isProcessing
}) => {
  
  if (status === 'cancelled') {
    return (
      <Card>
        <Card.Body title="Assinatura Cancelada">
          <p className="text-muted">
            Sua assinatura está inativa. Para acessar os benefícios novamente, 
            por favor realize uma nova assinatura na página de planos.
          </p>
        </Card.Body>
      </Card>
    );
  }

  return (
    <Card>
      <Card.Body title="Gerenciar Assinatura"> {/* [cite: 27] */}
        <p className={styles.actionText}>
          Você pode alterar o status da sua assinatura a qualquer momento.
        </p>
      </Card.Body>

      <Card.Actions> {/*  - Usando a área dedicada a botões do Card */}
        <div className={styles.actionsWrapper}>
          
          {/* Toggle Pause/Reactivate */}
          {(status === 'authorized' || status === 'active') ? (
            <button 
              onClick={onPause}
              disabled={isProcessing}
              className={`${styles.btnAction} ${styles.pause}`}
            >
              <i className="fas fa-pause" /> Pausar Assinatura
            </button>
          ) : (
            <button 
              onClick={onReactivate}
              disabled={isProcessing}
              className={`${styles.btnAction} ${styles.reactivate}`}
            >
              <i className="fas fa-play" /> Reativar Assinatura
            </button>
          )}

          {/* Cancel */}
          <button 
            onClick={onCancel}
            disabled={isProcessing}
            className={`${styles.btnAction} ${styles.cancel}`}
          >
            <i className="fas fa-times" /> Cancelar Assinatura
          </button>

        </div>
      </Card.Actions>
    </Card>
  );
};