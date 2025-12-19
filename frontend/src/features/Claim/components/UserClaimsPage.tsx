import React, { useState, useEffect, useCallback } from "react";
import styles from "../styles/ClaimsLayout.module.scss";
import { ClaimChat } from "../../../components/MercadoPago/Claim/components/ClaimChat";
import { ClaimsList } from "../../../components/MercadoPago/Claim/components/ClaimsList";
import { ClaimService } from "../../../components/MercadoPago/Claim/services/claim.service";
import type { ClaimSummary } from "../../../components/MercadoPago/Claim/types/claims.type";
import { Modal } from "../../../components/Modal/Modal";
import { Sidebar } from "../../../components/SideBar/components/Sidebar";
import type { SidebarItem } from "../../../components/SideBar/types/sidebar.types";

const USER_MENU: SidebarItem[] = [
  { id: "my-claims", label: "Minhas Reclamações", icon: "fas fa-list-alt" },
  { id: "help", label: "Ajuda / FAQ", icon: "fas fa-question-circle" },
];

export const UserClaimsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState("my-claims");
  const [claims, setClaims] = useState<ClaimSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [selectedClaimId, setSelectedClaimId] = useState<number | null>(null);

  const loadMyClaims = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await ClaimService.user.getMyClaims();
      setClaims(data);
    } catch (error) {
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab === "my-claims") {
      loadMyClaims();
    }
  }, [activeTab, loadMyClaims]);

  return (
    <div className={styles.layoutContainer}>
      <Sidebar
        items={USER_MENU}
        activeItemId={activeTab}
        onItemClick={(item) => setActiveTab(String(item.id))}
        logo={
          <span className="text-xl font-bold p-4 text-white">Minha Conta</span>
        }
      />

      <main className={styles.mainContent}>
        <h1 className={styles.pageTitle}>
          {activeTab === "my-claims" ? "Minhas Disputas" : "Central de Ajuda"}
        </h1>

        {activeTab === "my-claims" ? (
          <ClaimsList
            data={claims} // CORREÇÃO: Passa direto, sem 'as any'
            isLoading={isLoading}
            userRole="admin"
            // CORREÇÃO: O objeto agora é garantidamente um ClaimSummary, então tem internalId
            onViewDetails={(claim) => setSelectedClaimId(claim.internalId)}
          />
        ) : (
          <div className="bg-white p-6 rounded shadow-sm">
            <h3 className="font-bold mb-2">Como funciona o processo?</h3>
            <p className="text-gray-600">
              Se você tiver problemas com um curso, abra uma disputa. Nossa
              equipe responderá em até 24h.
            </p>
          </div>
        )}
      </main>

      <Modal
        isOpen={!!selectedClaimId}
        onClose={() => setSelectedClaimId(null)}
        title="Conversa com o Suporte"
        size="large"
      >
        {selectedClaimId && <ClaimChat claimId={selectedClaimId} role="user" />}
      </Modal>
    </div>
  );
};
