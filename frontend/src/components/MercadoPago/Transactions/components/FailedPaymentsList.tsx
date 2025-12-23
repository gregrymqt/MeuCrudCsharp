import React, { useState, useMemo } from 'react';
import { usePaymentHistory } from '../hooks/usePaymentHistory'; // [cite: 2]
import type { PaymentItems } from '../types/transactions.type'; // [cite: 2]
import styles from '../styles/FailedPaymentsList.module.scss'; // [cite: 2]
import { type TableColumn, Table } from '../../../Table/Table'; // [cite: 3]

export const FailedPaymentsList: React.FC = () => {
    // 1. Consumimos o hook existente para buscar todos os pagamentos
    const { payments, loading, error, refetch } = usePaymentHistory(); // [cite: 4]

    // 2. Estado para controlar a aba ativa ('rejected' ou 'refunded')
    const [activeTab, setActiveTab] = useState<'rejected' | 'refunded'>('rejected'); // [cite: 5, 6]

    // 3. Filtramos os dados com base na aba selecionada
    const filteredData = useMemo(() => {
        return payments.filter((item) => {
            const status = item.status?.toLowerCase() || ''; // [cite: 6]

            if (activeTab === 'rejected') {
                // Considera recusados (rejected) ou cancelados (cancelled)
                return status === 'rejected' || status === 'cancelled'; // [cite: 7]
            }
            if (activeTab === 'refunded') {
                // Considera estornados
                return status === 'refunded' || status === 'partially_refunded'; // [cite: 7]
            }
            return false; // [cite: 8]
        });
    }, [payments, activeTab]);

    // 4. Definição das colunas da Tabela
    const columns: TableColumn<PaymentItems>[] = [ // [cite: 9]
        {
            header: 'Data',
            width: '20%',
            render: (item) => new Date(item.createdAt).toLocaleDateString('pt-BR') // [cite: 9]
        },
        {
            header: 'Descrição / Motivo',
            width: '40%',
            // CORREÇÃO 1: Removemos o 'item' ou usamos '_' para indicar que não é usado, 
            // pois a lógica depende apenas de 'activeTab'.
            render: () => (activeTab === 'rejected' ? 'Pagamento Recusado' : 'Reembolso Processado') // [cite: 10]
        },
        {
            header: 'Valor',
            width: '20%',
            render: (item) =>
                new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.amount) // [cite: 11]
        },
        {
            header: 'Status',
            width: '20%',
            render: (item) => (
                // CORREÇÃO 2: Uso do 'styles' para acessar as classes do CSS Module
                <span className={`${styles.statusBadge} ${styles[item.status?.toLowerCase()] || ''}`}>
                    {item.status === 'rejected' ? 'Recusado' : 'Estornado'}
                </span> // [cite: 12]
            )
        }
    ];

    if (error) {
        return (
            // CORREÇÃO 2: Substituição da string fixa pelo objeto styles
            <div className={styles.errorContainer}>
                <p>Erro: {error}</p>
                <button onClick={refetch}>Tentar novamente</button> {/* [cite: 13] */}
            </div>
        );
    }

    return (
        // CORREÇÃO 2: Substituição das strings fixas pelo objeto styles
        <div className={styles.failedPaymentsContainer}>
            <h3>Pagamentos Recusados e Estornos</h3>

            {/* Navegação por Abas */}
            <div className={styles.tabsHeader}>
                <button
                    // CORREÇÃO 2: Aplicação condicional usando styles
                    className={activeTab === 'rejected' ? styles.active : ''}
                    onClick={() => setActiveTab('rejected')} // [cite: 15]
                >
                    Recusados
                </button>
                <button
                    className={activeTab === 'refunded' ? styles.active : ''}
                    onClick={() => setActiveTab('refunded')} // [cite: 16]
                >
                    Estornados
                </button>
            </div>

            {/* Tabela Reutilizável */}
            <div className={styles.tabContent}>
                <Table<PaymentItems> // [cite: 17]
                    data={filteredData}
                    columns={columns}
                    isLoading={loading} // [cite: 18]
                    keyExtractor={(item) => item.id} // [cite: 18]
                    emptyMessage={
                        activeTab === 'rejected'
                            ? "Nenhum pagamento recusado encontrado." // [cite: 19]
                            : "Nenhum estorno encontrado." // [cite: 20]
                    }
                />
            </div>
        </div>
    );
};