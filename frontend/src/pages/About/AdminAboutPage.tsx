import React, { useState } from "react";
import styles from "./styles/AdminAboutPage.module.css";

import { Sidebar } from "../../components/SideBar/components/Sidebar";
import type { SidebarItem } from "../../components/SideBar/types/sidebar.types";
import { TeamMemberForm } from "../../features/about/components/Members/TeamMemberForm";
import { TeamMemberList } from "../../features/about/components/Members/TeamMemberList";
import { AboutSectionForm } from "../../features/about/components/Section/AboutSectionForm";
import { AboutSectionList } from "../../features/about/components/Section/AboutSectionList";

// Importamos o NOVO hook de leitura
import { useAboutData } from "../../features/about/hooks/useAboutData";

// Hooks de escrita (CRUD) continuam os mesmos
import { useAboutSection } from "../../features/about/hooks/useAboutSection";
import { useTeamMembers } from "../../features/about/hooks/useTeamMembers";

import type {
  AboutSectionData,
  TeamMember,
} from "../../features/about/types/about.types";

// --- CONFIGURAÇÃO DA SIDEBAR ---
const sidebarItems: SidebarItem[] = [
  { id: "forms", label: "Gerenciar Cadastros", icon: "fas fa-pen-square" },
  { id: "list", label: "Visualizar Dados", icon: "fas fa-table" },
];

export const AdminAboutPage: React.FC = () => {
  // --- ESTADOS DE NAVEGAÇÃO ---
  const [activeSidebarId, setActiveSidebarId] = useState<string | number>(
    "list"
  );
  const [activeTab, setActiveTab] = useState<"sections" | "members">(
    "sections"
  );

  // --- NOVO: HOOK DE LEITURA (Substitui os states manuais e o useEffect antigo) ---
  const {
    sections,
    teamMembers,
    isLoading: isFetching,
    refreshData, // Usaremos isso para atualizar a lista após criar/editar/deletar
  } = useAboutData();

  // --- ESTADO DE EDIÇÃO ---
  const [editingSection, setEditingSection] = useState<
    AboutSectionData | undefined
  >(undefined);
  const [editingMember, setEditingMember] = useState<TeamMember | undefined>(
    undefined
  );

  // --- HOOKS DE AÇÃO (CRUD) ---
  const {
    createSection,
    updateSection,
    deleteSection,
    isLoading: loadingSection,
  } = useAboutSection();

  const {
    addMember,
    updateMember,
    deleteMember,
    isLoading: loadingMember,
  } = useTeamMembers();

  // --- HANDLERS DE EDIÇÃO ---
  const handleEditSection = (item: AboutSectionData) => {
    setEditingSection(item);
    setActiveTab("sections");
    setActiveSidebarId("forms");
  };

  const handleEditMember = (item: TeamMember) => {
    setEditingMember(item);
    setActiveTab("members");
    setActiveSidebarId("forms");
  };

  // --- HANDLER DE SUCESSO ---
  const handleSuccess = () => {
    refreshData(); // Chamamos a função do hook para recarregar os dados
    setEditingSection(undefined);
    setEditingMember(undefined);
    setActiveSidebarId("list");
  };

  // --- RENDERIZAÇÃO CONDICIONAL ---
  const renderContent = () => {
    const isFormsMode = activeSidebarId === "forms";

    // 1. Renderizar Formulários
    if (isFormsMode) {
      if (activeTab === "sections") {
        return (
          <AboutSectionForm
            initialData={editingSection}
            onSubmit={(data) => {
              if (editingSection) {
                updateSection(editingSection.id, data, handleSuccess);
              } else {
                createSection(data, handleSuccess);
              }
            }}
            isLoading={loadingSection}
          />
        );
      } else {
        return (
          <TeamMemberForm
            initialData={editingMember}
            onSubmit={(data) => {
              if (editingMember) {
                updateMember(editingMember.id, data, handleSuccess);
              } else {
                addMember(data, handleSuccess);
              }
            }}
            isLoading={loadingMember}
          />
        );
      }
    }

    // 2. Renderizar Listas (Usando dados vindos do hook useAboutData)
    else {
      if (activeTab === "sections") {
        return (
          <AboutSectionList
            data={sections} // Variável do hook
            isLoading={isFetching}
            onEdit={handleEditSection}
            onDelete={(id) => deleteSection(id, refreshData)} // Passamos refreshData como callback
          />
        );
      } else {
        return (
          <TeamMemberList
            data={teamMembers} // Variável do hook
            isLoading={isFetching}
            onEdit={handleEditMember}
            onDelete={(id) => deleteMember(id, refreshData)} // Passamos refreshData como callback
          />
        );
      }
    }
  };

  return (
    <div className={styles.pageContainer}>
      <Sidebar
        items={sidebarItems}
        activeItemId={activeSidebarId}
        onItemClick={(item) => {
          setActiveSidebarId(item.id);
          if (item.id === "list") {
            setEditingSection(undefined);
            setEditingMember(undefined);
          }
        }}
        logo={<h3>Admin Panel</h3>}
      >
        <div
          style={{
            padding: "1rem",
            borderTop: "1px solid #ddd",
            color: "#666",
          }}
        >
          <small>Versão 1.0</small>
        </div>
      </Sidebar>

      <main className={styles.contentArea}>
        <header>
          <h1>
            {activeSidebarId === "forms" ? "Gerenciamento" : "Visualização"}
          </h1>
          <p>Administre as seções e membros da página Sobre Nós.</p>
        </header>

        <nav className={styles.tabsContainer}>
          <button
            className={`${styles.tabButton} ${
              activeTab === "sections" ? styles.active : ""
            }`}
            onClick={() => setActiveTab("sections")}
          >
            <i className="fas fa-layer-group" style={{ marginRight: 8 }}></i>
            Seções do Site
          </button>

          <button
            className={`${styles.tabButton} ${
              activeTab === "members" ? styles.active : ""
            }`}
            onClick={() => setActiveTab("members")}
          >
            <i className="fas fa-users" style={{ marginRight: 8 }}></i>
            Equipe
          </button>
        </nav>

        <div className="fade-in">{renderContent()}</div>
      </main>
    </div>
  );
};
