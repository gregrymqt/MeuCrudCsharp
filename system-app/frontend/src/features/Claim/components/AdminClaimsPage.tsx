import React, { useState, useEffect, useCallback } from "react";
import styles from "../styles/ClaimsLayout.module.scss"; // Verifique se o nome do arquivo CSS está correto
import { ClaimChat } from "../../../components/MercadoPago/Claim/components/ClaimChat";
import { ClaimsList } from "../../../components/MercadoPago/Claim/components/ClaimsList";
import { ClaimService } from "../../../components/MercadoPago/Claim/services/claim.service";
import type { ClaimSummary } from "../../../components/MercadoPago/Claim/types/claims.type";
import { Modal } from "../../../components/Modal/Modal";
import { Sidebar } from "../../../components/SideBar/components/Sidebar";
import type { SidebarItem } from "../../../components/SideBar/types/sidebar.types";

// Menu da Sidebar do Admin
const MENU_ITEMS: SidebarItem[] = [
  { id: "opened", label: "Caixa de Entrada", icon: "fas fa-inbox" },
  { id: "mediation", label: "Em Mediação (MP)", icon: "fas fa-gavel" },
  { id: "closed", label: "Resolvidos / Histórico", icon: "fas fa-history" },
];

export const AdminClaimsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState("opened");
  const [claims, setClaims] = useState<ClaimSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Estado para controlar qual disputa está aberta no Modal
  const [selectedClaimId, setSelectedClaimId] = useState<number | null>(null);

  const loadClaims = useCallback(async () => {
    setIsLoading(true);
    try {
      // Mapeia a aba visual para o filtro da API
      const statusFilter =
        activeTab === "closed"
          ? "closed"
          : activeTab === "mediation"
          ? "mediation"
          : "opened";

      const response = await ClaimService.admin.getAll(1, "", statusFilter);
      setClaims(response.claims);
    } catch (error) {
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  }, [activeTab]);

  // Busca dados quando a função loadClaims mudar (que depende de activeTab)
  useEffect(() => {
    loadClaims();
  }, [loadClaims]);

  return (
    <div className={styles.layoutContainer}>
      <Sidebar
        items={MENU_ITEMS}
        activeItemId={activeTab}
        onItemClick={(item) => setActiveTab(String(item.id))}
        logo={<h3 className="p-4 font-bold text-white">Admin Painel</h3>}
      >
        <div className="p-4 text-xs text-white opacity-70">
          Greg Company © 2024
        </div>
      </Sidebar>

      <main className={styles.mainContent}>
        <h1 className={styles.pageTitle}>Gerenciamento de Disputas</h1>

        <ClaimsList
          data={claims} // CORREÇÃO: Passa direto, sem 'as any'
          isLoading={isLoading}
          userRole="user"
          // CORREÇÃO: Usa internalId
          onViewDetails={(claim) => setSelectedClaimId(claim.internalId)}
        />
      </main>

      <Modal
        isOpen={!!selectedClaimId}
        onClose={() => {
          setSelectedClaimId(null);
          loadClaims();
        }}
        title="Detalhes e Chat"
        size="large"
      >
        {selectedClaimId && (
          <ClaimChat claimId={selectedClaimId} role="admin" />
        )}
      </Modal>
    </div>
  );
};
