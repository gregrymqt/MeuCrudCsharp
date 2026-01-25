// src/features/Claim/components/ClaimsList.tsx

import React from 'react';
import { ClaimStatusBadge } from './ClaimStatusBadge';
// MUDANÇA: Importamos ClaimSummary ao invés de Claim
import { type ClaimSummary } from '../types/claims.type'; 
import styles from '../styles/ClaimsList.module.scss';
import type { ClaimStatus } from '../../../../types/models';
import { type TableColumn, Table } from '../../../Table/Table';

interface ClaimsListProps {
  data: ClaimSummary[]; // MUDANÇA: Agora aceita o Summary
  isLoading: boolean;
  onViewDetails: (claim: ClaimSummary) => void;
  userRole: 'admin' | 'user';
}

export const ClaimsList: React.FC<ClaimsListProps> = ({ 
  data, 
  isLoading, 
  onViewDetails
}) => {
  
  // MUDANÇA: Colunas tipadas com ClaimSummary
  const columns: TableColumn<ClaimSummary>[] = [
    {
      header: 'ID MP',
      accessor: 'mpClaimId', // MUDANÇA: claimId -> mpClaimId
      width: '120px',
      render: (item) => <span className={styles.monoFont}>#{item.mpClaimId}</span>
    },
    {
      header: 'Data',
      width: '150px',
      // MUDANÇA: dataCreated -> dateCreated
      render: (item) => new Date(item.dateCreated).toLocaleDateString('pt-BR') 
    },
    {
      header: 'Tipo',
      accessor: 'type',
      width: '150px',
      render: (item) => <span className={styles.typeTag}>{item.type || 'Geral'}</span>
    },
    {
      header: 'Status',
      width: '150px',
      // Convertendo string para Enum se necessário, ou passando direto se o badge aceitar string
      render: (item) => <ClaimStatusBadge status={item.status as unknown as ClaimStatus} /> 
    },
    {
      header: 'Ações',
      width: '100px',
      render: (item) => (
        <button 
          className={styles.actionBtn} 
          onClick={() => onViewDetails(item)}
          aria-label="Ver detalhes"
        >
          Ver Detalhes
        </button>
      )
    }
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2>Disputas e Reclamações</h2>
      </div>

      <div className={styles.tableWrapper}>
        <Table<ClaimSummary> 
          data={data}
          columns={columns}
          // MUDANÇA: id -> internalId
          keyExtractor={(item) => item.internalId} 
          isLoading={isLoading}
          emptyMessage="Nenhuma reclamação encontrada."
        />
      </div>
    </div>
  );
};