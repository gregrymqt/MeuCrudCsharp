import React, { useState } from 'react';
import { Card } from '../../../../components/Card/Card'; // [cite: 1]
import { MercadoPagoBrick } from '../../../../components/Payment/components/Credit-Card/components/MercadoPagoBrick'; // [cite: 3]
import styles from './SubscriptionManager.module.scss'; // [cite: 2]

// Types e Hooks
import { type SubscriptionDetails } from '../../types/profile.types'; // [cite: 2]
import { useSubscriptionActions } from '../../hooks/useSubscriptionActions'; // [cite: 3]
import { useRefundNotification } from '../../hooks/useRefundNotification'; // <--- NOVO: Hook do Socket

interface SubscriptionProps {
  data: SubscriptionDetails;
  onRefresh: () => void; // Callback para recarregar a tela após ação [cite: 4]
}

export const SubscriptionManager: React.FC<SubscriptionProps> = ({ data, onRefresh }) => {
  // Hook de ações (API REST)
  const { changeStatus, changeCard, requestRefund, loading } = useSubscriptionActions(onRefresh);
  
  // Hook de Notificação (WebSocket) - Ouve o status do reembolso em tempo real
  const { refundStatus } = useRefundNotification(onRefresh);

  // Estados locais para controlar a UI (Toggles)
  const [showCardForm, setShowCardForm] = useState(false); // [cite: 5]
  const [showRefund, setShowRefund] = useState(false); // [cite: 5]

  // Helpers visuais
  const isPaused = data.status === 'paused'; // [cite: 6]
  const isActive = data.status === 'active' || data.status === 'authorized'; // [cite: 7]
  
  // Formatação de Moeda
  const formattedPrice = new Intl.NumberFormat('pt-BR', { 
    style: 'currency', 
    currency: 'BRL' 
  }).format(data.amount); // [cite: 8]

  // Handler intermediário para o Mercado Pago
  const handleCardUpdate = async (token: string) => { // [cite: 9]
    await changeCard(token);
    setShowCardForm(false); // Fecha o form se der sucesso [cite: 10]
  };

  return (
    <Card className={styles.subscriptionCard}>
      <Card.Body title="Gerenciar Assinatura">
        
        {/* === INFO DO PLANO === */}
        <div className={styles.planDetails}>
          <div className={styles.info}>
            <div className={styles.headerRow}>
              <h5 className={styles.planName}>{data.planName}</h5>
              
              {/* Badge de Status Dinâmico */}
              {/* Adicionado tratamento de null/undefined para data.status */}
              <span className={`${styles.badge} ${data.status ? styles[data.status] : ''}`}> 
                {data.status === 'active' ? 'ATIVO' : 
                 data.status === 'paused' ? 'PAUSADO' : 
                 data.status === 'cancelled' ? 'CANCELADO' : (data.status?.toUpperCase() || 'DESCONHECIDO')}
              </span>
            </div>
            
            {data.nextBillingDate && (
              <small className={styles.billingDate}>
                Próxima cobrança: {new Date(data.nextBillingDate).toLocaleDateString()}
              </small>
            )}
            
            <div className={styles.cardInfo}>
              Cartão final: **** {data.lastFourCardDigits}
            </div>
          </div>
          
          <div className={styles.planPrice}>
             {formattedPrice} <span className={styles.period}>/mês</span>
          </div>
        </div>

        <hr className={styles.divider} />

        {/* === BOTÕES DE AÇÃO DO STATUS === */}
        <div className={styles.actions}>
          {isActive && (
            <>
              <button 
                onClick={() => changeStatus('paused')}
                disabled={loading} 
                className={`${styles.btnAction} ${styles.pause}`}
              >
                <i className="fas fa-pause"></i> Pausar
              </button>
              <button 
                onClick={() => changeStatus('cancelled')}
                disabled={loading} 
                className={`${styles.btnAction} ${styles.cancel}`}
              >
                <i className="fas fa-times"></i> Cancelar
              </button>
            </>
          )}

          {isPaused && (
            <button 
              onClick={() => changeStatus('active')}
              disabled={loading} 
              className={`${styles.btnAction} ${styles.reactivate}`}
            >
              <i className="fas fa-play"></i> Reativar Assinatura
            </button>
          )}
        </div>

        {/* === ÁREA DE TROCA DE CARTÃO & REEMBOLSO === */}
        <div className={styles.managementArea}>
            
            {/* Toggle Cartão */}
            <button 
              className={styles.toggleBtn}
              onClick={() => { setShowCardForm(!showCardForm); setShowRefund(false); }} // [cite: 21]
            >
              <span><i className="fas fa-credit-card"></i> Alterar Cartão de Crédito</span>
              <i className={`fas fa-chevron-${showCardForm ? 'up' : 'down'}`}></i>
            </button>
            
            {showCardForm && (
              <div className={styles.formContainer}>
                 <p className={styles.instructionText}>Insira os dados do novo cartão:</p>
                 {/* Componente Global do Mercado Pago */}
                 <MercadoPagoBrick 
                   amount={1} 
                   onSubmit={handleCardUpdate} 
                 />
              </div>
            )}

            {/* Toggle Reembolso */}
            <button 
              className={styles.toggleBtn}
              onClick={() => { setShowRefund(!showRefund); setShowCardForm(false); }} // [cite: 25]
            >
               <span><i className="fas fa-undo"></i> Solicitar Reembolso</span>
               <i className={`fas fa-chevron-${showRefund ? 'up' : 'down'}`}></i>
            </button>

            {showRefund && (
              <div className={styles.refundContainer}>
                
                {/* Lógica de UI baseada no WebSocket (Substituindo o estático) */}
                {refundStatus === 'completed' ? (
                  <div className="alert alert-success text-center">
                    <i className="fas fa-check-circle fa-2x mb-2"></i>
                    <p className="m-0">Reembolso processado com sucesso!</p>
                  </div>
                ) : (
                  <>
                    <p className={styles.warningText}>
                       <strong>Atenção:</strong> O reembolso só é permitido em até 7 dias após a cobrança. 
                       Seu acesso será cortado imediatamente.
                    </p>
                    <button 
                      onClick={requestRefund} 
                      // Bloqueia se a API REST estiver carregando OU se o Socket estiver processando
                      disabled={loading || refundStatus === 'processing'}
                      className={`${styles.btnAction} ${styles.confirmRefund}`}
                    >
                      {refundStatus === 'processing' ? 'Processando...' : 'Confirmar Reembolso'}
                    </button>
                  </>
                )}
              </div>
            )}
        </div>

      </Card.Body>
    </Card>
  );
};