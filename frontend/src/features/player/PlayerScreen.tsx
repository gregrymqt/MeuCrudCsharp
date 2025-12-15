import React from 'react';
import { useParams, useNavigate } from 'react-router-dom'; // Necessário para pegar o ID da URL

import { VideoPlayerFrame } from './components/PlayerFrame/VideoPlayerFrame';
import { VideoMetadata } from './components/MetaData/VideoMetadata';
import { useVideoPlayer } from './hooks/useVideoPlayer'; // O hook que criamos
import styles from './PlayerScreen.module.scss';

export const PlayerScreen: React.FC = () => {
  // 1. Pega o ID da rota (ex: /watch/guid-do-video)
  const { id } = useParams<{ id: string }>(); 
  const navigate = useNavigate();

  // 2. Consome o Hook
  const { 
    video, 
    isLoading, 
    error, 
    isPlaying, 
    handleStart, 
    videoRef 
  } = useVideoPlayer(id);

  // 3. Tratamento de Estados de Carga e Erro
  if (isLoading) {
    return <div className={styles.loadingState}>Carregando vídeo...</div>;
  }

  if (error || !video) {
    return (
      <div className={styles.errorState}>
        <p>{error || 'Vídeo não encontrado'}</p>
        <button onClick={() => navigate(-1)}>Voltar</button>
      </div>
    );
  }

  // 4. Renderização da Tela
  return (
    <main className={styles.screenContainer}>
      <header className={styles.topNav}>
        <button onClick={() => navigate(-1)} className={styles.backButton}>
          &lt; Voltar
        </button>
        {/* Título truncado no mobile para não quebrar layout */}
        <span className={styles.navTitle}>{video.title}</span>
      </header>

      <section className={styles.videoSection}>
        {/* Passamos todas as props necessárias para o frame funcionar */}
        <VideoPlayerFrame 
          video={video} 
          isPlaying={isPlaying} 
          onStart={handleStart} 
          videoRef={videoRef} 
        />
      </section>

      <section className={styles.infoSection}>
        <VideoMetadata 
          data={video}
          onNext={() => console.log('Lógica de Próxima Aula')}
          onPrev={undefined} 
        />
      </section>
    </main>
  );
};