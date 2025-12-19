import React, { useState, useMemo } from "react";
import { Sidebar } from "../../../components/SideBar/components/Sidebar";
import { useAuth } from "../../auth/hooks/useAuth"; // [cite: 3]
import { ProfileMapper } from "../utils/profile.mapper"; // O Helper criado acima
import { PROFILE_SIDEBAR_ITEMS } from "../config/sidebarConfig";
import type { SidebarItem } from "../../../components/SideBar/types/sidebar.types";

// Sub-componentes
import { ProfileInfo } from "../components/info/ProfileInfo"; // [cite: 2]
import { PaymentHistory } from "../components/history/PaymentHistory"; // [cite: 1]
import { SubscriptionManager } from "../components/subscription/SubscriptionManager"; // [cite: 2]

import styles from "../styles/ProfileDashboard.module.scss";

export const ProfileDashboard: React.FC = () => {
  const [activeTab, setActiveTab] = useState<string>("info"); // [cite: 6]

  // 1. Consumimos o Hook de Auth
  // refreshSession: Atualiza os dados do usuário no cookie (chama /auth/me)
  const { user, refreshSession } = useAuth();

  // 2. Transformação de Dados (Memoizado para performance)
  // Converte o objeto do Backend para o objeto da UI usando o Helper
  const subscriptionData = useMemo(() => {
    return ProfileMapper.toSubscriptionDetails(user);
  }, [user]);

  // 3. Função de Renderização
  const renderContent = () => {
    // Loading state simples se o user ainda não carregou do storage
    if (!user)
      return <div className={styles['loading-message']}>Carregando dados...</div>; // [cite: 12]

    switch (activeTab) {
      case "info":
        return (
          <ProfileInfo
            subscription={subscriptionData} // Passamos o dado tratado [cite: 13]
          />
        );

      case "payments":
        // Histórico busca seus próprios dados via API, independente do user session [cite: 14]
        return <PaymentHistory />;

      case "subscription":
        return subscriptionData ? (
          <SubscriptionManager
            data={subscriptionData}
            // AQUI ESTÁ A MÁGICA:
            // Quando o usuário pausa/cancela/troca cartão dentro deste componente,
            // chamamos refreshSession para buscar os dados atualizados no back e salvar no cookie.
            onRefresh={refreshSession} // [cite: 16, 64]
          />
        ) : (
          <div className={styles['no-subscription-alert']}>
            Você não possui uma assinatura ativa no momento. Veja nossos planos!{" "}
            {/* [cite: 17] */}
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className={styles['profile-dashboard']}>
      <Sidebar
        items={PROFILE_SIDEBAR_ITEMS}
        activeItemId={activeTab}
        onItemClick={(item: SidebarItem) => setActiveTab(String(item.id))}
        logo={<h4>Minha Conta</h4>}
      />

      <main className={styles['profile-main']}>
        <div className={styles['profile-container']}>
          <h2 className={styles['page-title']}>
            {PROFILE_SIDEBAR_ITEMS.find((i) => i.id === activeTab)?.label}
          </h2>
          {renderContent()}
        </div>
      </main>
    </div>
  );
};
