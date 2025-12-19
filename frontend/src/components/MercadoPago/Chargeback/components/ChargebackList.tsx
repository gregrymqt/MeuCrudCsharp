import { useState } from 'react';
import { type TableColumn, Table } from '../../../Table/Table';
import { useChargebackDetails } from '../hooks/useChargebackDetails';
import { useChargebacks } from '../hooks/useChargebacks';
import type { ChargebackSummary } from '../types/chargeback.type';
import { ChargebackDetailModal } from './ChargebackDetailModal';
import { type FilterFormData, ChargebackFilter } from './ChargebackFilter';
import styles from '../styles/ChargebackList.module.scss';


export const ChargebackList = () => {
  // 1. Hook de Listagem
  const { 
    chargebacks, 
    loading: listLoading, 
    pagination, 
    setPage, 
    setSearchTerm, 
    setStatusFilter 
  } = useChargebacks();

  // 2. Estado para Modal e Hook de Detalhes
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const { details, loading: detailsLoading } = useChargebackDetails(selectedId);

  // Manipuladores
  const handleFilter = (data: FilterFormData) => {
    setSearchTerm(data.searchTerm);
    setStatusFilter(data.status);
  };

  const openModal = (id: string) => setSelectedId(id);
  const closeModal = () => setSelectedId(null);

  // 3. Definição das Colunas da Tabela
  const columns: TableColumn<ChargebackSummary>[] = [
    { 
      header: 'Cliente', 
      accessor: 'customer',
      width: '30%'
    },
    { 
      header: 'Valor', 
      render: (item) => `R$ ${item.amount.toFixed(2)}`,
      width: '15%'
    },
    { 
      header: 'Data', 
      render: (item) => new Date(item.date).toLocaleDateString(),
      width: '15%'
    },
    { 
      header: 'Status', 
      render: (item) => {
        // Exemplo simples de classe baseada no texto do status
        const statusClass = item.status.toLowerCase().includes('novo') ? 'novo' : 'aguardando';
        return <span className={`${styles['status-badge']} ${styles[statusClass]}`}>{item.status}</span>;
      },
      width: '20%'
    },
    { 
      header: 'Ações',
      render: (item) => (
        <button 
            className={styles['btn-details']} 
            onClick={() => openModal(item.id)}
        >
          Ver Detalhes
        </button>
      ),
      width: '15%'
    }
  ];

  return (
    <div className={styles['chargeback-page']}>
      <header>
        <h1>Contestações (Chargebacks)</h1>
        <p>Gerencie disputas e envie evidências para o Mercado Pago.</p>
      </header>

      {/* Componente de Filtro */}
      <ChargebackFilter onFilter={handleFilter} isLoading={listLoading} />

      {/* Tabela Genérica */}
      <Table<ChargebackSummary>
        data={chargebacks}
        columns={columns}
        keyExtractor={(item) => item.id}
        isLoading={listLoading}
        emptyMessage="Nenhuma contestação encontrada."
      />

      {/* Paginação Simples */}
      <div className={styles['pagination-controls']}>
        <button 
          disabled={!pagination.hasPreviousPage} 
          onClick={() => setPage(pagination.currentPage - 1)}
        >
          <i className="fas fa-chevron-left"></i> Anterior
        </button>
        <span>Página {pagination.currentPage} de {pagination.totalPages}</span>
        <button 
          disabled={!pagination.hasNextPage} 
          onClick={() => setPage(pagination.currentPage + 1)}
        >
          Próxima <i className="fas fa-chevron-right"></i>
        </button>
      </div>

      {/* Modal de Detalhes */}
      <ChargebackDetailModal 
        isOpen={!!selectedId}
        onClose={closeModal}
        data={details}
        isLoading={detailsLoading}
      />
    </div>
  );
};