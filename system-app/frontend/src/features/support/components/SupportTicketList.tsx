import React, { useEffect, useMemo } from 'react';
import { useSupport } from '../hooks/useSupport';
import  { ActionMenu } from '../../../components/ActionMenu/ActionMenu';
import { type TableColumn, Table } from '../../../components/Table/Table';
import type { SupportTicketStatus, SupportTicket } from '../types/support.types';
import styles from '../styles/SupportTicketList.module.scss';

export const SupportTicketList: React.FC = () => {
  const { tickets, loading, fetchTicketsPaginated, updateStatus } = useSupport();

  // Busca os dados ao montar o componente
  useEffect(() => {
    fetchTicketsPaginated();
  }, [fetchTicketsPaginated]);

  // --- Renderizadores Auxiliares ---

  // Renderiza a Badge colorida
  const renderStatusBadge = (status: SupportTicketStatus) => {
    let className = styles.badge;
    let label = status;

    switch (status) {
      case 'Open':
        className += ` ${styles.open}`;
        label = 'Open';
        break;
      case 'InProgress':
        className += ` ${styles.inProgress}`;
        label = 'InProgress';
        break;
      case 'Closed':
        className += ` ${styles.closed}`;
        label = 'Closed';
        break;
    }

    return <span className={className}>{label}</span>;
  };

  // --- Definição das Colunas da Tabela ---
  const columns = useMemo<TableColumn<SupportTicket>[]>(() => [
    {
      header: 'Data',
      width: '120px',
      render: (item) => new Date(item.createdAt).toLocaleDateString('pt-BR'),
    },
    {
      header: 'Assunto',
      accessor: 'context',
      width: '150px',
    },
    {
      header: 'Mensagem',
      accessor: 'explanation',
      render: (item) => (
        <span title={item.explanation}>
          {item.explanation.length > 50 
            ? `${item.explanation.substring(0, 50)}...` 
            : item.explanation}
        </span>
      )
    },
    {
      header: 'Status',
      width: '120px',
      render: (item) => renderStatusBadge(item.status),
    },
    {
      header: 'Ações',
      width: '80px',
      render: (item) => (
        <ActionMenu>
            {/* Lógica condicional: O que o admin pode fazer com este ticket? */}
            
            {item.status === 'Open' && (
                <button 
                    className={`${styles.actionItem} ${styles.warning}`}
                    onClick={() => updateStatus(item.id, 'InProgress')}
                >
                    <i className="fas fa-play-circle"></i> Iniciar Atendimento
                </button>
            )}

            {item.status === 'InProgress' && (
                <button 
                    className={`${styles.actionItem} ${styles.success}`}
                    onClick={() => updateStatus(item.id, 'Closed')}
                >
                    <i className="fas fa-check-circle"></i> Finalizar Ticket
                </button>
            )}

            {/* Opção de reabrir caso tenha sido fechado por engano */}
            {item.status === 'Closed' && (
                <button 
                    className={styles.actionItem}
                    onClick={() => updateStatus(item.id, 'InProgress')}
                >
                    <i className="fas fa-undo"></i> Reabrir
                </button>
            )}
        </ActionMenu>
      ),
    },
  ], [updateStatus]);

  return (
    <div className={styles.container}>
      <header className={styles.header}>
        <div>
            <h2>Gerenciamento de Suporte</h2>
            <small className="text-muted">Visualize e atenda as solicitações dos usuários</small>
        </div>
        
        <button className={styles.refreshBtn} onClick={() => fetchTicketsPaginated(1, true)} disabled={loading}>
          <i className={`fas fa-sync-alt ${loading ? 'fa-spin' : ''}`}></i> 
          Atualizar
        </button>
      </header>

      <Table<SupportTicket>
        data={tickets}
        columns={columns}
        isLoading={loading}
        keyExtractor={(item) => item.id}
        emptyMessage="Nenhum ticket de suporte encontrado."
      />
    </div>
  );
};