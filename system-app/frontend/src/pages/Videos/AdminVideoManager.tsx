import React, { useState, useEffect, useMemo } from "react";

// Styles
import styles from "./AdminVideoManager.module.scss";
import { Sidebar } from "../../components/SideBar/components/Sidebar";
import type { SidebarItem } from "../../components/SideBar/types/sidebar.types";
import { VideoForm } from "../../features/Videos/components/VideoForm";
import { VideoList } from "../../features/Videos/components/VideoList";
import { VideoPlayer } from "../../features/Videos/components/VideoPlayer";
import { useAdminVideos } from "../../features/Videos/hooks/useAdminVideos";
import type { VideoTab, VideoFormData } from "../../features/Videos/types/video-manager.types";
import { useCourses } from "../../features/course/Allow/hooks/useCourses";
import { AlertService } from "../../shared/services/alert.service";
import type { Video, Course } from "../../types/models";



export const AdminVideoManager: React.FC = () => {
  // 1. Instanciando os Hooks de Dados
  const {
    videos,
    loading: loadingVideos,
    fetchVideos,
    createVideo,
    updateVideo,
    deleteVideo,
  } = useAdminVideos();

  // Precisamos dos cursos para preencher o <select> do formulário
  const { courses, refresh: refreshCourses } = useCourses();

  // Converter CourseRowUI para Course para compatibilidade com VideoForm
  const coursesForForm = useMemo<Course[]>(() => {
    return courses.map(courseRow => ({
      id: courseRow.id,
      publicId: String(courseRow.id), // CourseRowUI não tem publicId, usando id como fallback
      name: courseRow.categoryName,
      description: '', // CourseRowUI não tem description
      videos: [] // Não precisamos dos videos aqui
    }));
  }, [courses]);

  // 2. Estados de UI (Controle de Abas e Seleção)
  const [activeTab, setActiveTab] = useState<VideoTab>("list");
  const [selectedVideo, setSelectedVideo] = useState<Video | null>(null);
  const [videoToWatch, setVideoToWatch] = useState<Video | null>(null);

  // 3. Carregar dados iniciais
  useEffect(() => {
    fetchVideos();
    refreshCourses();
  }, [fetchVideos]);

  // Itens da Sidebar
  const sidebarItems: SidebarItem[] = [
    { id: "list", label: "Visualizar Dados", icon: "fas fa-list" },
    { id: "form", label: "Formulário", icon: "fas fa-plus-circle" },
    { id: "player", label: "Visualizar Vídeo", icon: "fas fa-play-circle" },
  ];

  // --- Handlers ---

  const handleTabChange = (item: SidebarItem) => {
    const tabId = item.id as VideoTab;

    if (tabId === "list" || tabId === "form") {
      setSelectedVideo(null);
    }
    setActiveTab(tabId);
  };

  const handleEdit = (video: Video) => {
    setSelectedVideo(video);
    setActiveTab("form");
  };

  const handleWatch = (video: Video) => {
    setVideoToWatch(video);
    setActiveTab("player");
  };

  const handleDelete = async (publicId: string) => {
    // O hook já gerencia o AlertService.confirm internamente
    const success = await deleteVideo(publicId);
    if (success) {
      fetchVideos(); // Garante sincronia
    }
  };

  const handleFormSubmit = async (data: VideoFormData) => {
    let success = false;

    // 1. Extraindo os Arquivos corretamente (sem usar 'any')
    const thumbnailFile =
      data.thumbnail && data.thumbnail.length > 0
        ? data.thumbnail[0]
        : undefined;

    // [CORREÇÃO] Agora o TypeScript reconhece 'videoFile' direto no objeto 'data'
    const videoFile =
      data.videoFile && data.videoFile.length > 0
        ? data.videoFile[0]
        : undefined;

    if (selectedVideo) {
      // --- UPDATE (Edição) ---
      // Na edição, geralmente não obrigamos reenviar o vídeo, apenas atualizamos dados
      success = await updateVideo(selectedVideo.publicId, {
        title: data.title,
        description: data.description,
        thumbnailFile: thumbnailFile,
      });
    } else {
      // --- CREATE (Criação) ---

      // [EXPLICAÇÃO DA LÓGICA]
      // Esta validação garante que o usuário enviou ALGUMA fonte de vídeo.
      // Se ele não preencheu a URL E também não selecionou um arquivo, mostramos erro.
      if (!videoFile && !data.videoUrl) {
        AlertService.error(
          "Atenção",
          "Para criar um vídeo, você deve fornecer uma URL (Youtube) OU fazer upload de um arquivo."
        );
        return; // Interrompe a função aqui, não chama o backend
      }

      // Se passou na validação, envia para o hook
      success = await createVideo({
        title: data.title,
        description: data.description,
        courseId: data.courseId,
        videoFile: videoFile as File, // Casting seguro pois validamos acima
        thumbnailFile: thumbnailFile,
      });
    }

    if (success) {
      setActiveTab("list");
      setSelectedVideo(null);
      fetchVideos();
    }
  };

  return (
    <div className={styles.adminContainer}>
      <Sidebar
        items={sidebarItems}
        activeItemId={activeTab}
        onItemClick={handleTabChange}
        logo={<h3 style={{ color: "#fff", padding: "0 20px" }}>Admin Panel</h3>}
      />

      <main className={styles.contentArea}>
        <div className={styles.contentWrapper}>
          {activeTab === "list" && (
            <VideoList
              videos={videos}
              isLoading={loadingVideos}
              onEdit={handleEdit}
              onDelete={handleDelete}
              onWatch={handleWatch}
              onNewClick={() => {
                setSelectedVideo(null);
                setActiveTab("form");
              }}
              onRefresh={() => fetchVideos()}
            />
          )}

          {activeTab === "form" && (
            <VideoForm
              initialData={selectedVideo}
              courses={coursesForForm} // Usando cursos convertidos
              isLoading={loadingVideos}
              onSubmit={handleFormSubmit}
              onCancel={() => setActiveTab("list")}
            />
          )}

          {activeTab === "player" && (
            <VideoPlayer
              video={videoToWatch}
              onBack={() => setActiveTab("list")}
            />
          )}
        </div>
      </main>
    </div>
  );
};
