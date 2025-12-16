import React from 'react';
import styles from './PaginationControls.module.scss';

interface PaginationProps {
  currentPage: number;
  totalPages: number;
  onNext: () => void;
  onPrev: () => void;
}

export const PaginationControls: React.FC<PaginationProps> = ({ 
  currentPage, totalPages, onNext, onPrev 
}) => {
  if (totalPages <= 1) return null;

  return (
    <div className={styles.pagination}>
      <button onClick={onPrev} disabled={currentPage === 1}>
        &lt; Anterior
      </button>
      <span>{currentPage} / {totalPages}</span>
      <button onClick={onNext} disabled={currentPage === totalPages}>
        Próximo &gt;
      </button>
    </div>
  );
};