// src/pages/Payment/PaymentLayout.tsx
import React, { useState, useMemo } from "react";
import styles from "./PaymentLayout.module.scss";

// Components

import { Sidebar } from "../../components/SideBar/components/Sidebar";

// Hooks & Types

import type { SidebarItem } from "../../components/SideBar/types/sidebar.types";
import { CreditCardPayment } from "../../components/Payment/components/Credit-Card/CreditCardPayment";
import { PixPayment } from "../../components/Payment/components/Pix/PixPayment";
import { usePreference } from "../../components/Payment/components/Preference/hooks/usePreference";
import type {
  PaymentLayoutProps,
  PaymentMethodId,
} from "../../components/Payment/types/payment.types";

export const PaymentLayout: React.FC<PaymentLayoutProps> = ({
  plan,
  userParams,
}) => {
  const [activeTab, setActiveTab] = useState<PaymentMethodId>("credit-card");
  const [paymentSuccess, setPaymentSuccess] = useState(false);

  // --- BUSCA O PREFERENCE ID ---
  // Só buscamos se o plano existir e tiver valor.
  // O hook gerencia o loading internamente.
  const {
    preferenceId,
    loading: loadingPreference,
    error: errorPreference,
  } = usePreference(plan.amount);

  // --- LÓGICA DE SIDEBAR (MANTIDA) ---
  const sidebarItems: SidebarItem[] = useMemo(() => {
    const items: SidebarItem[] = [];
    const isMensal = plan.frequency === "monthly";

    if (isMensal) {
      items.push({ id: "pix", label: "PIX", icon: "fas fa-qrcode" });
    }
    items.push({
      id: "credit-card",
      label: "Cartão de Crédito",
      icon: "fas fa-credit-card",
    });

    return items;
  }, [plan.frequency]);

  const currentTabExists = sidebarItems.find((i) => i.id === activeTab);
  if (!currentTabExists && sidebarItems.length > 0) {
    setActiveTab(sidebarItems[0].id as PaymentMethodId);
  }

  const handleSuccess = () => {
    setPaymentSuccess(true);
  };

  // --- RENDERIZAÇÃO ---
  const renderContent = () => {
    if (paymentSuccess) {
      return (
        <div className={styles.successWrapper}>
          <i className="fas fa-check-circle"></i>
          <h2>Tudo certo!</h2>
          <p>Seu plano {plan.name} foi ativado.</p>
        </div>
      );
    }

    switch (activeTab) {
      case "pix":
        return (
          <PixPayment
            amount={plan.amount}
            planName={plan.name}
            userParams={userParams}
            onPaymentSuccess={handleSuccess}
          />
        );

      case "credit-card":
        // UI/UX: Tratamento de Loading da Preferência
        if (loadingPreference) {
          return (
            <div className={styles.loadingContainer}>
              <div className="spinner-border text-primary" role="status"></div>
              <p>Iniciando ambiente seguro...</p>
            </div>
          );
        }

        // UI/UX: Tratamento de Erro ao buscar ID
        if (errorPreference || !preferenceId) {
          return (
            <div className="alert alert-warning">
              <i className="fas fa-exclamation-triangle"></i>
              <p>Não foi possível carregar o formulário de pagamento.</p>
              <button
                className="btn btn-outline-primary btn-sm"
                onClick={() => window.location.reload()}
              >
                Tentar novamente
              </button>
            </div>
          );
        }

        return (
          <CreditCardPayment
            planId={plan.id}
            planName={plan.name}
            amount={plan.amount}
            mode={plan.frequency === "monthly" ? "payment" : "subscription"} // Exemplo de lógica
            preferenceId={preferenceId} // Passando o ID gerado pelo controller
            onPaymentSuccess={handleSuccess}
          />
        );

      default:
        return <div>Selecione um método.</div>;
    }
  };

  return (
    <div className={styles.layoutContainer}>
      <Sidebar
        items={sidebarItems}
        activeItemId={activeTab}
        onItemClick={(item) => setActiveTab(item.id as PaymentMethodId)}
        logo={<h3>Greg Pay</h3>}
      >
        <div style={{ padding: "20px", color: "#fff", cursor: "pointer" }}>
          <i className="fas fa-arrow-left"></i> Voltar
        </div>
      </Sidebar>

      <main className={styles.mainContent}>
        <div className={styles.orderSummary}>
          <div className={styles.planInfo}>
            <span className={styles.label}>Plano:</span>
            <span className={styles.value}>{plan.name}</span>
          </div>
          <div className={styles.priceInfo}>
            <span className={styles.label}>Total:</span>
            <span className={styles.value}>R$ {plan.amount.toFixed(2)}</span>
          </div>
        </div>

        <div className={styles.contentCard}>
          {!paymentSuccess && (
            <h2>
              {activeTab === "pix" ? "Pagamento à Vista" : "Dados do Pagamento"}
            </h2>
          )}
          {renderContent()}
        </div>
      </main>
    </div>
  );
};
