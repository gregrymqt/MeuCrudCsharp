import React from "react";
import { type Video, VideoStatus } from "../../../../types/models"; 
import { type VideoListProps } from "../types/video-manager.types";
import styles from "../styles/VideoList.module.scss";
import { ActionMenu } from "../../../../components/ActionMenu/ActionMenu";
import { type TableColumn, Table } from "../../../../components/Table/Table";
import { ProcessingBadge } from "./ProcessingBadge"; //

export const VideoList: React.FC<VideoListProps> = ({
  videos,
  isLoading,
  onEdit,
  onDelete,
  onWatch,
  onNewClick,
  onRefresh
}) => {
  
  // Função auxiliar para status simples (Agora será utilizada)
  const getStatusBadge = (status: number) => {
    switch (status) {
      case VideoStatus.Available:
        return (
          <span className={`${styles.statusBadge} ${styles.available}`}>
            Disponível
          </span>
        );
      case VideoStatus.Processing:
        return (
          <span className={`${styles.statusBadge} ${styles.processing}`}>
            Processando
          </span>
        );
      case VideoStatus.Error:
        return (
          <span className={`${styles.statusBadge} ${styles.error}`}>Erro</span>
        );
      default:
        return <span>-</span>;
    }
  };

  const columns: TableColumn<Video>[] = [
    {
      header: "Preview",
      width: "80px",
      render: (video) => (
        <button
          className={styles.playBtn}
          onClick={() => onWatch(video)}
          title="Assistir Vídeo"
        >
          <i className="fas fa-play"></i>
        </button>
      ),
    },
    { header: "Título", accessor: "title", width: "30%" },
    {
      header: "Curso",
      width: "25%",
      render: (video) => video.course?.name || "Sem Curso",
    },
    { header: "Duração", accessor: "duration", width: "15%" },
    {
      header: "Status",
      width: "20%",
      render: (video) => {
        // 1. Prioridade: Se estiver processando, mostra o Badge com Socket/Progresso
        if (video.status === VideoStatus.Processing) {
          return (
            <ProcessingBadge
              storageIdentifier={video.storageIdentifier}
              status={video.status}
              onProcessComplete={onRefresh} //
            />
          );
        }

        // 2. Outros status: Usa a função auxiliar (Resolvendo o erro do ESLint)
        return getStatusBadge(video.status);
      },
    },
    {
      header: "Ações",
      width: "10%",
      render: (video) => (
        <ActionMenu
          onEdit={() => onEdit(video)}
          onDelete={() => onDelete(video.publicId)}
        />
      ),
    },
  ];

  return (
    <div className={styles.listContainer}>
      <div className={styles.header}>
        <h2>Gerenciar Vídeos</h2>
        <button className={styles.createBtn} onClick={onNewClick}>
          <i className="fas fa-plus"></i> Novo Vídeo
        </button>
      </div>

      <Table
        data={videos}
        columns={columns}
        keyExtractor={(v) => v.publicId}
        isLoading={isLoading}
        emptyMessage="Nenhum vídeo encontrado."
      />
    </div>
  );
};