import React, { useState } from "react";
import styles from "./styles/ChargebackManager.module.scss";
import { ChargebackList } from "../../components/MercadoPago/Chargeback/components/ChargebackList";
import { Sidebar } from "../../components/SideBar/components/Sidebar";
import type { SidebarItem } from "../../components/SideBar/types/sidebar.types";

// Definindo as abas disponíveis
const NAV_ITEMS: SidebarItem[] = [
  { id: "dashboard", label: "Dashboard", icon: "fas fa-chart-pie" },
  { id: "list", label: "Contestações", icon: "fas fa-list-alt" },
  { id: "settings", label: "Configurações", icon: "fas fa-cog" },
];

export const ChargebackManager: React.FC = () => {
  // Estado para controlar qual aba está ativa (Padrão: list)
  const [activeTabId, setActiveTabId] = useState<string | number>("list");

  // Função para renderizar o conteúdo com base na aba
  const renderContent = () => {
    switch (activeTabId) {
      case "list":
        return <ChargebackList />;
      case "dashboard":
        return (
          <div className={styles.placeholderState}>
            <i className="fas fa-chart-pie"></i>
            <h2>Dashboard em construção</h2>
            <p>Em breve você verá estatísticas aqui.</p>
          </div>
        );
      case "settings":
        return (
          <div className={styles.placeholderState}>
            <i className="fas fa-cog"></i>
            <h2>Configurações</h2>
            <p>Ajuste suas preferências de disputa aqui.</p>
          </div>
        );
      default:
        return null;
    }
  };

  return (
    <div className={styles.layoutContainer}>
      {/* A Sidebar já contém o Header Mobile internamente.
        Passamos o logo e os itens de navegação.
      */}
      <Sidebar
        items={NAV_ITEMS}
        activeItemId={activeTabId}
        onItemClick={(item) => setActiveTabId(item.id)}
        logo={<span className={styles.logoText}>Greg Company</span>}
      >
        {/* Conteúdo do rodapé da Sidebar (ex: Logout) */}
        <div className={styles.sidebarFooter}>
          <small>v1.0.0</small>
        </div>
      </Sidebar>

      {/* Área Principal de Conteúdo */}
      <main className={styles.mainContent}>{renderContent()}</main>
    </div>
  );
};
