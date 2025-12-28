import React, { useMemo } from "react";
import { useNavigate } from "react-router-dom"; // Importante para navegação

import styles from "../styles/ProfileDashboard.module.scss";
import { Sidebar } from "../../components/SideBar/components/Sidebar";
import type { SidebarItem } from "../../components/SideBar/types/sidebar.types";
import { useAuth } from "../../features/auth/hooks/useAuth";
import { ProfileInfo } from "../../features/profile/components/ProfileInfo";
import { getProfileSidebarItems } from "../../features/profile/config/sidebarConfig";

export const ProfileDashboard: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();

  // Gera os itens da sidebar dinamicamente baseada nas flags do usuário
  const sidebarItems = useMemo(() => getProfileSidebarItems(user), [user]);

  // Função para gerenciar o clique na Sidebar
  const handleSidebarClick = (item: SidebarItem) => {
    const id = String(item.id);

    switch (id) {
      case "info":
        // Mantém na tela atual (pode não fazer nada ou resetar scroll)
        break;

      case "payments":
        // Redireciona para a rota dedicada de transações
        navigate("/transacoes");
        break;

      case "subscription":
        // Redireciona para a rota dedicada de assinaturas
        navigate("/assinaturas");
        break;

      default:
        break;
    }
  };

  if (!user) {
    return <div className={styles["loading-message"]}>Carregando dados...</div>;
  }

  return (
    <div className={styles["profile-dashboard"]}>
      <Sidebar
        items={sidebarItems}
        activeItemId="info" // Sempre mantemos "info" ativo visualmente pois as outras saem da tela
        onItemClick={handleSidebarClick}
        logo={<h4>Minha Conta</h4>}
      />

      <main className={styles["profile-main"]}>
        <div className={styles["profile-container"]}>
          <h2 className={styles["page-title"]}>Meu Perfil</h2>

          {/* Agora só renderizamos o ProfileInfo. 
              As outras telas são rotas externas. */}
          <ProfileInfo />
        </div>
      </main>
    </div>
  );
};
