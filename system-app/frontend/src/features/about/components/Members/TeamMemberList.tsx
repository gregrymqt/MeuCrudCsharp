import React from 'react';

import styles from '../../styles/AboutLists.module.scss';
import { ActionMenu } from '../../../../components/ActionMenu/ActionMenu';
import { type TableColumn, Table } from '../../../../components/Table/Table';
import type { TeamMember } from '../../types/about.types';

interface TeamListProps {
  data: TeamMember[];
  isLoading: boolean;
  onEdit: (member: TeamMember) => void;
  onDelete: (id: number | string) => void;
}

export const TeamMemberList: React.FC<TeamListProps> = ({ 
  data, 
  isLoading, 
  onEdit, 
  onDelete 
}) => {

  const columns: TableColumn<TeamMember>[] = [
    {
      header: 'Foto',
      width: '70px',
      render: (item) => (
        <img 
          src={item.photoUrl} 
          alt={`Foto de ${item.name}`} 
          className={styles.memberAvatar}
          // Fallback visual caso a imagem quebre (opcional)
          onError={(e) => {
            (e.target as HTMLImageElement).src = 'https://via.placeholder.com/50?text=User';
          }}
        />
      )
    },
    {
      header: 'Nome',
      accessor: 'name',
      width: '30%'
    },
    {
      header: 'Cargo',
      accessor: 'role',
      width: '30%'
    },
    {
      header: 'Redes',
      width: '100px',
      render: (item) => (
        <div style={{ display: 'flex', gap: '8px', color: '#666' }}>
          {item.linkedinUrl && <i className="fab fa-linkedin" title="LinkedIn"></i>}
          {item.githubUrl && <i className="fab fa-github" title="GitHub"></i>}
          {!item.linkedinUrl && !item.githubUrl && <span>-</span>}
        </div>
      )
    },
    {
      header: 'Ações',
      width: '80px',
      render: (item) => (
        <div className={styles.actionsCell}>
          <ActionMenu 
            onEdit={() => onEdit(item)} 
            onDelete={() => onDelete(item.id)} 
          />
        </div>
      )
    }
  ];

  return (
    <div className={styles.listContainer}>
      <Table<TeamMember>
        data={data}
        columns={columns}
        isLoading={isLoading}
        keyExtractor={(item) => item.id}
        emptyMessage="Nenhum membro da equipe encontrado."
      />
    </div>
  );
};