import React from 'react';
import styles from './VideoPlayerFrame.module.scss';
import type { PlayerUI } from '../../types/player.type';

interface VideoPlayerFrameProps {
  video: PlayerUI;
  onStart: () => void;
  isPlaying: boolean;
  // Nova prop obrigatória para o HLS funcionar
  videoRef: React.RefObject<HTMLVideoElement | null>; 
}

export const VideoPlayerFrame: React.FC<VideoPlayerFrameProps> = ({ 
  video, 
  onStart, 
  isPlaying,
  videoRef 
}) => {
  return (
    <div className={styles.playerWrapper}>
      <div className={styles.aspectRatioBox}>
        {isPlaying ? (
          // O Player Real é renderizado aqui
          <video 
            ref={videoRef}
            className={styles.nativePlayer} // Classe para width/height 100%
            controls 
            autoPlay 
            playsInline // Essencial para não abrir fullscreen forçado no iOS [cite: 35]
          />
        ) : (
          // Estado de Capa (Poster)
          <div 
            className={styles.posterContainer} 
            onClick={onStart}
            style={{ backgroundImage: `url(${video.thumbnailUrl})` }}
          >
            <div className={styles.playOverlay}>
              <button className={styles.bigPlayButton} aria-label="Reproduzir vídeo">
                ▶
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};