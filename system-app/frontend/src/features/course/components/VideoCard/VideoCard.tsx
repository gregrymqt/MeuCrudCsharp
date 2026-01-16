import React from 'react';
import styles from './VideoCard.module.scss';
import type { VideoCardUI } from '../../types/course.type';

interface VideoCardProps {
  data: VideoCardUI;
  onClick?: (video: VideoCardUI) => void;
}

export const VideoCard: React.FC<VideoCardProps> = ({ data, onClick }) => {
  return (
    <div 
      className={styles.cardContainer} 
      onClick={() => onClick && onClick(data)}
      role="button"
      tabIndex={0}
    >
      <div className={styles.imageWrapper}>
        <img 
          src={data.thumbnailUrl} 
          alt={data.title} 
          className={styles.thumbnail}
          loading="lazy"
        />
        
        {/* Badge de Duração (Canto inferior direito) */}
        <span className={styles.durationBadge}>
          {data.durationFormatted}
        </span>

        {/* Ícone de Play (Centralizado, aparece no Hover) */}
        <div className={styles.playOverlay}>
          <i className="fas fa-play"></i>
        </div>

        {/* Badge Opcional "Novo" */}
        {data.isNew && <span className={styles.newBadge}>Novo</span>}
      </div>

      <div className={styles.infoArea}>
        <h4 className={styles.title}>{data.title}</h4>
      </div>
    </div>
  );
};