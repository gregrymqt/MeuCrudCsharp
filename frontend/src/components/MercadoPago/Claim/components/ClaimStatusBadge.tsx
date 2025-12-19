import React from 'react';
import styles from '../styles/ClaimStatusBadge.module.scss';
import  { ClaimStatus } from '../../../../types/models';

interface Props {
  status: ClaimStatus;
}

export const ClaimStatusBadge: React.FC<Props> = ({ status }) => {
  // Mapeamento de Cores e Textos
  const getStatusConfig = (status: ClaimStatus) => {
    switch (status) {
      case ClaimStatus.Novo:
        return { label: 'Novo', className: styles.info };
      case ClaimStatus.EmAnalise:
        return { label: 'Em Análise', className: styles.warning };
      case ClaimStatus.ResolvidoGanhamos:
        return { label: 'Ganho', className: styles.success };
      case ClaimStatus.ResolvidoPerdemos:
        return { label: 'Perdido', className: styles.danger };
      default:
        return { label: 'Desconhecido', className: styles.default };
    }
  };

  const config = getStatusConfig(status);

  return (
    <span className={`${styles.badge} ${config.className}`}>
      {config.label}
    </span>
  );
};