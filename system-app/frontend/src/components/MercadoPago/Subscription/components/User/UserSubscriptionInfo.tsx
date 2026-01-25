import React from 'react';
import styles from '../../styles/UserSubscriptionInfo.module.scss';
import { Card } from '../../../../Card/Card';
import type { SubscriptionDetailsDto } from '../../types/userSubscription.type';

interface SubscriptionInfoProps {
    data: SubscriptionDetailsDto;
}

export const SubscriptionInfo: React.FC<SubscriptionInfoProps> = ({ data }) => {
    // Helpers
    const formatCurrency = (val: number) =>
        new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(val);

    const formatDate = (dateStr: string | null) =>
        dateStr ? new Date(dateStr).toLocaleDateString('pt-BR') : '-';

    const translateStatus = (status: string | null) => {
        const map: Record<string, string> = {
            authorized: 'Ativa',
            paused: 'Pausada',
            cancelled: 'Cancelada'
        };
        return map[status || ''] || status || '-';
    };

    return (
        // Reutilizando seu Card Genérico
        <Card className="mb-4">
            <Card.Body title="Detalhes do Plano"> {/* [cite: 27] */}

                <div className={styles.dataGrid}>
                    <div className={styles.dataItem}>
                        <label>Plano</label>
                        <span>{data.planName || 'N/A'}</span>
                    </div>

                    <div className={styles.dataItem}>
                        <label>Status</label>
                        <span className={`${styles.badge} ${styles[data.status || '']}`}>
                            {translateStatus(data.status)}
                        </span>
                    </div>

                    <div className={styles.dataItem}>
                        <label>Valor</label>
                        <span>{formatCurrency(data.amount)}</span>
                    </div>

                    <div className={styles.dataItem}>
                        <label>Cartão</label>
                        <span>{data.lastFourCardDigits ? `•••• ${data.lastFourCardDigits}` : 'N/A'}</span>
                    </div>

                    <div className={styles.dataItem}>
                        <label>Próxima Cobrança</label>
                        <span>{formatDate(data.nextBillingDate)}</span>
                    </div>
                </div>

            </Card.Body>
        </Card>
    );
};