    import React from 'react';
    import styles from '../styles/PaymentHistory.module.scss';
    import { type TableColumn, Table } from '../../../../components/Table/Table';
    import { ActionMenu } from '../../../ActionMenu/ActionMenu';
    import { usePaymentHistory } from '../hooks/usePaymentHistory';
    import { useRefund } from '../hooks/useRefund';
    import { useRefundNotification } from '../hooks/useRefundNotification';
    import type { PaymentItems } from '../types/transactions.type';

    export const PaymentHistory: React.FC = () => {
        const { payments, loading, error, refetch } = usePaymentHistory();
        const { requestRefund, isProcessing } = useRefund();

        // --- LIGA O SOCKET ---
        // Assim que o socket receber "completed", ele avisa na tela e roda o refetch
        useRefundNotification(refetch);

        // (O resto do componente permanece igual, focando na tabela)
        const getStatusConfig = (status: string) => {
            const s = status?.toLowerCase() || '';
            if (s === 'approved' || s === 'paid') return { label: 'Pago', className: styles.statusPAID };
            if (s === 'pending' || s === 'authorized') return { label: 'Pendente', className: styles.statusPENDING };
            if (s === 'refunded') return { label: 'Estornado', className: styles.statusREFUNDED };
            return { label: 'Falhou', className: styles.statusFAILED };
        };

        const columns: TableColumn<PaymentItems>[] = [
            {
                header: 'Data',
                render: (item) => new Date(item.createdAt).toLocaleDateString('pt-BR'),
                width: '15%'
            },
            {
                header: 'Descrição',
                accessor: 'description',
                render: (item) => item.description || 'Assinatura',
                width: '35%'
            },
            {
                header: 'Valor',
                width: '15%',
                render: (item) => `R$ ${item.amount.toFixed(2)}`
            },
            {
                header: 'Status',
                width: '15%',
                render: (item) => {
                    const config = getStatusConfig(item.status);
                    return <span className={config.className}>{config.label}</span>;
                }
            },
            {
                header: 'Ações',
                width: '100px',
                render: (item) => {
                    const canRefund = (item.status === 'approved' || item.status === 'paid');

                    return (
                        <ActionMenu>
                            {canRefund && (
                                <button
                                    onClick={() => requestRefund(item.id)}
                                    className={styles.actionBtn}
                                    title="Solicitar Reembolso"
                                    disabled={isProcessing}
                                >
                                    <i className="fas fa-undo"></i> Estornar
                                </button>
                            )}
                            <button className={styles.actionBtn} title="Ver Recibo">
                                <i className="fas fa-file-invoice"></i> Detalhes
                            </button>
                        </ActionMenu>
                    );
                }
            }
        ];

        if (error) return <div className="alert alert-danger">{error}</div>;

        return (
            <div className={styles.tableContainer}>
                <h3 className="mb-4">Histórico de Pagamentos</h3>
                <Table<PaymentItems>
                    data={payments}
                    columns={columns}
                    isLoading={loading}
                    keyExtractor={(item) => item.id}
                    emptyMessage="Nenhum pagamento encontrado."
                />
            </div>
        );
    };