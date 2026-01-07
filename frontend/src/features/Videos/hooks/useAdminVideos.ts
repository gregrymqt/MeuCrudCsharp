// src/features/admin/videos/hooks/useAdminVideos.ts

import { useState, useCallback } from 'react';
import { VideoService } from '../services/video.service';
import { AlertService } from '../../../../shared/services/alert.service'; // 
import { ApiError } from '../../../../shared/services/api.service'; //
import type { Video } from '../../../../types/models';
import type { PaginatedResponse, CreateVideoParams, UpdateVideoParams } from '../types/video-manager.types';

export const useAdminVideos = () => {
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<PaginatedResponse<Video> | null>(null);

  // --- READ ---
  const fetchVideos = useCallback(async (page = 1, pageSize = 10) => {
    setLoading(true);
    try {
      const response = await VideoService.getAll({ page, pageSize });
      setData(response);
    } catch (error) {
      if (error instanceof ApiError && error.status === 404) {
        AlertService.error('Erro ao carregar', error.message);
      }
    } finally {
      setLoading(false);
    }
  }, []);

  // --- CREATE (Upload) ---
  const createVideo = async (params: CreateVideoParams): Promise<boolean> => {
    setLoading(true);
    try {
      // 1. Envia para o backend (inicia processamento)
      await VideoService.create(params);
      
      // 2. Feedback de Sucesso [cite: 36]
      await AlertService.success(
        'Upload Iniciado!', 
        'O vídeo foi enviado e está sendo processado. Ele aparecerá na lista em breve.'
      );
      
      return true;
    } catch (error) {
      const message = error instanceof ApiError ? error.message : 'Falha no upload do vídeo.';
      AlertService.error('Erro no Upload', message);
      return false;
    } finally {
      setLoading(false);
    }
  };

  // --- UPDATE ---
  const updateVideo = async (id: string, params: UpdateVideoParams): Promise<boolean> => {
    setLoading(true);
    try {
      await VideoService.update(id, params);
      
      // Atualização otimista local (opcional) ou feedback visual
      AlertService.success('Sucesso', 'Informações do vídeo atualizadas.');
      return true;
    } catch (error) {
      const message = error instanceof ApiError ? error.message : 'Erro ao atualizar.';
      AlertService.error('Erro', message);
      return false;
    } finally {
      setLoading(false);
    }
  };

  // --- DELETE ---
  const deleteVideo = async (id: string): Promise<boolean> => {
    // 1. Confirmação Visual usando o método confirm do AlertService
    const { isConfirmed } = await AlertService.confirm(
      'Excluir Vídeo?',
      'Isso removerá o vídeo, a thumbnail e todos os arquivos de streaming permanentemente.'
    );

    if (!isConfirmed) return false;

    setLoading(true);
    try {
      await VideoService.delete(id);
      
      AlertService.success('Deletado', 'Vídeo removido com sucesso.');
      
      // Atualiza lista removendo item localmente para evitar refresh imediato
      if (data) {
        setData({
          ...data,
          items: data.items.filter(v => v.id !== id)
        });
      }
      return true;
    } catch (error) {
      if (error instanceof ApiError) {
        AlertService.error('Erro', error.message);
        return false;
      }
      return false; // Adicionado para garantir que a função sempre retorne um booleano
    } finally {
      setLoading(false);
    }
  };

  return {
    videos: data?.items || [],
    pagination: {
      totalCount: data?.totalCount || 0,
      currentPage: data?.page || 1,
      pageSize: data?.pageSize || 10,
      totalPages: data ? Math.ceil(data.totalCount / data.pageSize) : 0
    },
    loading,
    fetchVideos,
    createVideo,
    updateVideo,
    deleteVideo
  };
};