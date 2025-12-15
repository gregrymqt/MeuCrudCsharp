import { useState, useEffect, useRef, useCallback } from 'react';
import Hls from 'hls.js';
import  { videoService } from '../services/video.service';
import type { PlayerUI, VideoDto } from '../types/player.type';


export const useVideoPlayer = (publicId: string | undefined) => {
  const [video, setVideo] = useState<PlayerUI | null>(null);
  const [internalData, setInternalData] = useState<VideoDto | null>(null); // Guarda dados técnicos (storageIdentifier)
  const [isLoading, setIsLoading] = useState(true);
  const [isPlaying, setIsPlaying] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Refs para manipular o DOM e a instância HLS
  const videoRef = useRef<HTMLVideoElement>(null);
  const hlsRef = useRef<Hls | null>(null);

  // 1. Busca os metadados do vídeo
  useEffect(() => {
    if (!publicId) return;

    const fetchVideoData = async () => {
      setIsLoading(true);
      try {
        const data = await videoService.getById(publicId);
        setInternalData(data);
        
        // Mapeia DTO para UI
        setVideo({
          id: data.publicId,
          title: data.title,
          description: data.description || '',
          thumbnailUrl: data.thumbnailUrl,
          durationFormatted: formatDuration(data.duration),
          courseTitle: data.courseTitle
        });
      } catch (err) {
        setError('Erro ao carregar informações do vídeo.');
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    };

    fetchVideoData();
  }, [publicId]);

  // 2. Lógica de Streaming (HLS) - Executa quando isPlaying vira true
  useEffect(() => {
    // Só inicia se o usuário deu play, se temos o elemento de vídeo e os dados
    if (!isPlaying || !videoRef.current || !internalData) return;

    const videoElement = videoRef.current;
    const manifestUrl = videoService.getManifestUrl(internalData.storageIdentifier);

    // Verifica suporte ao HLS.js (PC, Android) [cite: 12]
    if (Hls.isSupported()) {
      const hls = new Hls({
        startLevel: -1, // Auto quality [cite: 12]
        capLevelToPlayerSize: true
      });
      
      hls.loadSource(manifestUrl); // [cite: 13]
      hls.attachMedia(videoElement);

      hls.on(Hls.Events.MANIFEST_PARSED, () => {
        // Tenta dar play automático após carregar
        videoElement.play().catch(e => console.log('Autoplay bloqueado pelo browser', e));
      });

      hls.on(Hls.Events.ERROR, (event, data) => {
        if (data.fatal) {
           console.error('Erro fatal HLS:', data); // [cite: 15]
           // Aqui você poderia tentar recuperar o erro
        }
      });

      hlsRef.current = hls;
    } 
    // Fallback para Safari/iOS Nativo [cite: 16]
    else if (videoElement.canPlayType('application/vnd.apple.mpegurl')) {
      videoElement.src = manifestUrl;
      videoElement.addEventListener('loadedmetadata', () => {
        videoElement.play(); // [cite: 17]
      });
    }

    // Cleanup: Destruir HLS ao sair da página ou parar vídeo
    return () => {
      if (hlsRef.current) {
        hlsRef.current.destroy();
      }
    };
  }, [isPlaying, internalData]);

  // Função chamada pelo botão de Play da capa
  const handleStart = useCallback(() => {
    setIsPlaying(true);
  }, []);

  return {
    video,        // Dados para a UI (Titulo, Desc)
    isLoading,
    error,
    isPlaying,    // Se true, o componente deve renderizar a tag <video>
    handleStart,  // Função para ativar o player
    videoRef      // Ref para ligar na tag <video ref={videoRef} />
  };
};

// Helper simples para formatar duração (ajuste conforme o formato vindo do C#)
function formatDuration(timeSpan: string): string {
  // Exemplo básico assumindo "00:10:00" -> "10 min"
  return timeSpan || "00:00"; 
}