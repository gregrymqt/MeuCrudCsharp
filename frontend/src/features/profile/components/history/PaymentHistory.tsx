// features/profile/components/PaymentHistory.tsx
import React from 'react';
import styles from './PaymentHistory.module.scss';
import { type TableColumn, Table } from '../../../../components/Table/Table'; // [cite: 1]
import type { PaymentItem } from '../../types/profile.types'; // [cite: 2, 22]
import { usePaymentHistory } from '../../hooks/usePaymentHistory';

export const PaymentHistory: React.FC = () => {
  // Utilizamos o hook criado acima para buscar os dados
  const { payments, loading, error } = usePaymentHistory();

  // Helper para mapear o status da API para o estilo CSS e Label [cite: 4, 10, 11]
  const getStatusConfig = (status: string) => {
    // Normaliza para lowercase para evitar erros de comparação
    const s = status?.toLowerCase() || '';

    if (s === 'approved' || s === 'paid') {
      return { label: 'Pago', className: styles.statusPAID };
    }
    if (s === 'pending' || s === 'authorized') {
      return { label: 'Pendente', className: styles.statusPENDING };
    }
    // Default para failed/cancelled
    return { label: 'Falhou', className: styles.statusFAILED };
  };

  // Configuração das Colunas
  const columns: TableColumn<PaymentItem>[] = [
    { 
      header: 'Data', 
      // O tipo PaymentItem tem 'createdAt', não 'date' 
      render: (item) => new Date(item.createdAt).toLocaleDateString('pt-BR'),
      width: '20%'
    },
    { 
      header: 'Descrição', 
      accessor: 'description', // [cite: 3, 23]
      // Fallback caso description venha undefined
      render: (item) => item.description || 'Assinatura Mensal' 
    },
    { 
      header: 'Valor', 
      width: '20%',
      render: (item) => `R$ ${item.amount.toFixed(2)}` // [cite: 4]
    },
    { 
      header: 'Status',
      width: '20%',
      render: (item) => {
        const config = getStatusConfig(item.status);
        return (
          <span className={config.className}>
            {config.label}
          </span>
        );
      }
    }
  ];

  if (error) {
    return <div className="alert alert-danger">{error}</div>;
  }

  return (
    <div className={styles.tableContainer}>
      <h3 className="mb-4">Histórico de Pagamentos</h3>
      
      <Table<PaymentItem>
        data={payments}
        columns={columns}
        isLoading={loading}
        keyExtractor={(item) => item.id} // [cite: 5, 22]
        emptyMessage="Nenhum pagamento encontrado."
      />
    </div>
  );
};