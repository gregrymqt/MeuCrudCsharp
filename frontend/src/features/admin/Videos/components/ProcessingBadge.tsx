import React from 'react';
import { useVideoProcessing } from '../hooks/useVideoProcessing';
import styles from '../styles/ProcessingBadge.module.scss';

interface Props {
  storageIdentifier: string;
  status: number;
  onProcessComplete: () => void;
}

export const ProcessingBadge: React.FC<Props> = ({ storageIdentifier, status, onProcessComplete }) => {
  const { progress, statusMessage, isProcessing } = useVideoProcessing(
    storageIdentifier, 
    status, 
    onProcessComplete
  );

  if (!isProcessing) {
    // Se terminou, retorna null para que a tabela renderize o status novo (Available/Error)
    // ou renderiza um estado estático temporário
    return null; 
  }

  return (
    <div className={styles.badgeContainer}>
      <div className={styles.statusRow}>
        <span>{statusMessage || 'Processando...'}</span>
        <strong>{progress}%</strong>
      </div>
      
      {/* Barra de Progresso Simples */}
      <div className={styles.progressTrack}>
        <div 
          className={styles.progressBar}
          style={{ width: `${progress}%` }} 
        />
      </div>
    </div>
  );
};