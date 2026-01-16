import React from 'react';
import styles from '../styles/VideoPlayer.module.scss';
import { type VideoPlayerProps } from '../types/video-manager.types';

export const VideoPlayer: React.FC<VideoPlayerProps> = ({ video, onBack }) => {
  if (!video) {
    return (
      <div className={styles.emptyState}>
        <i className="fas fa-film"></i>
        <h3>Nenhum vídeo selecionado</h3>
        <p>Selecione um vídeo na aba "Visualizar Dados" para assistir aqui.</p>
        <button onClick={onBack} style={{ marginTop: '10px', padding: '8px 16px', cursor: 'pointer' }}>
            Ir para Lista
        </button>
      </div>
    );
  }

  return (
    <div className={styles.playerContainer}>
      <div className={styles.videoHeader}>
        <button onClick={onBack} className={styles.backBtn}>
          <i className="fas fa-arrow-left"></i> Voltar
        </button>
        <h2>{video.title}</h2>
      </div>

      <div className={styles.videoFrame}>
        {/* Lógica simples para renderizar iframe ou video tag */}
        {video.storageIdentifier.includes('youtube') || video.storageIdentifier.includes('vimeo') ? (
           <iframe 
             src={video.storageIdentifier} 
             title={video.title} 
             allowFullScreen 
           />
        ) : (
           <video controls src={video.storageIdentifier} />
        )}
      </div>

      <div className={styles.details}>
        <h3>Descrição</h3>
        <p>{video.description || 'Sem descrição.'}</p>
        
        {video.course && (
            <p><strong>Curso:</strong> {video.course.name}</p>
        )}
      </div>
    </div>
  );
};