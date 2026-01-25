import React from 'react';
import styles from '../styles/CourseSkeleton.module.scss';

export const CourseSkeleton: React.FC = () => {
  return (
    <div className={styles.skeletonContainer}>
      {/* Criamos 3 linhas falsas para simular o carregamento */}
      {[1, 2, 3].map((i) => (
        <div key={i} className={styles.skeletonRow}>
          <div className={styles.skeletonTitle}></div>
          <div className={styles.skeletonCarousel}>
            <div className={styles.skeletonCard}></div>
            <div className={styles.skeletonCard}></div>
            <div className={styles.skeletonCard}></div>
          </div>
        </div>
      ))}
    </div>
  );
};