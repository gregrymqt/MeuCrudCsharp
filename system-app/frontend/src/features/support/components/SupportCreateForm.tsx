import React, { useState, useEffect, useMemo, useRef } from "react";
import { useSupport } from "../hooks/useSupport";
import type {
  SupportTicket,
  SupportTicketStatus,
} from "../types/support.types";

import styles from "../styles/SupportTicketList.module.scss"; // Use o mesmo SCSS
import { ActionMenu } from "../../../components/ActionMenu/ActionMenu";
import {
  type FormField,
  GenericForm,
} from "../../../components/Form/GenericForm";
import { type TableColumn, Table } from "../../../components/Table/Table";

type TabOption = "list" | "search";

export const SupportCreateForm: React.FC = () => {
  const {
    tickets,
    currentTicket,
    loading,
    hasMore,
    page,
    fetchTicketsPaginated,
    fetchTicketById,
    updateStatus, // Supondo que você exportou isso do hook
  } = useSupport();

  const [activeTab, setActiveTab] = useState<TabOption>("list");

  // Ref para o "observador" de scroll infinito
  const observerTarget = useRef<HTMLDivElement>(null);

  // --- 1. EFEITO: Carregar primeira página ---
  useEffect(() => {
    if (activeTab === "list" && tickets.length === 0) {
      fetchTicketsPaginated(1, true);
    }
  }, [activeTab, fetchTicketsPaginated, tickets.length]);

  // --- 2. EFEITO: Scroll Infinito (Intersection Observer) ---
  useEffect(() => {
    if (activeTab !== "list") return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          fetchTicketsPaginated(page + 1, false);
        }
      },
      { threshold: 1.0 }
    );

    if (observerTarget.current) {
      observer.observe(observerTarget.current);
    }

    return () => observer.disconnect();
  }, [hasMore, loading, page, activeTab, fetchTicketsPaginated]);

  // --- CONFIGURAÇÃO DA BUSCA (Aba 2) ---
  const searchFields = useMemo(
    (): FormField<{ ticketId: string }>[] => [
      {
        name: "ticketId",
        label: "ID do Ticket",
        type: "text",
        placeholder: "Cole o ID aqui (ex: 650c...)",
        colSpan: 12,
        validation: { required: "O ID é obrigatório" },
      },
    ],
    []
  );

  // --- CONFIGURAÇÃO DA TABELA (Aba 1 & Resultado Busca) ---
  const renderStatusBadge = (status: SupportTicketStatus) => {
    // ... (Mesma lógica de badge anterior) ...
    return (
      <span className={`${styles.badge} ${styles[status.toLowerCase()]}`}>
        {status}
      </span>
    );
  };

  const columns = useMemo<TableColumn<SupportTicket>[]>(
    () => [
      {
        header: "Data",
        width: "120px",
        render: (i) => new Date(i.createdAt).toLocaleDateString("pt-BR"),
      },
      { header: "Assunto", accessor: "context", width: "150px" },
      {
        header: "Mensagem",
        render: (i) => (
          <span title={i.explanation}>{i.explanation.substring(0, 40)}...</span>
        ),
      },
      { header: "Status", render: (i) => renderStatusBadge(i.status) },
      {
        header: "Ações",
        render: (item) => (
          <ActionMenu>
            {/* ... Mesmos botões de ação do código anterior ... */}
            {/* Exemplo: */}
            {item.status === "Open" && (
              <button
                className={styles.actionItem}
                onClick={() => updateStatus(item.id, "InProgress")}
              >
                Atender
              </button>
            )}
          </ActionMenu>
        ),
      },
    ],
    [updateStatus]
  );

  return (
    <div className={styles.container}>
      {/* --- CABEÇALHO COM ABAS --- */}
      <header className={styles.header}>
        <div className="flex gap-4">
          <button
            className={`${styles.tabBtn} ${
              activeTab === "list" ? styles.active : ""
            }`}
            onClick={() => setActiveTab("list")}
          >
            <i className="fas fa-list me-2"></i> Lista Geral
          </button>

          <button
            className={`${styles.tabBtn} ${
              activeTab === "search" ? styles.active : ""
            }`}
            onClick={() => setActiveTab("search")}
          >
            <i className="fas fa-search me-2"></i> Buscar por ID
          </button>
        </div>

        {activeTab === "list" && (
          <button
            className={styles.refreshBtn}
            onClick={() => fetchTicketsPaginated(1, true)}
          >
            <i className="fas fa-sync-alt"></i>
          </button>
        )}
      </header>

      {/* --- ABA 1: LISTA COM SCROLL INFINITO --- */}
      {activeTab === "list" && (
        <>
          <Table<SupportTicket>
            data={tickets}
            columns={columns}
            keyExtractor={(item) => item.id}
            emptyMessage="Nenhum ticket encontrado."
          />

          {/* Elemento Sentinela para o Scroll */}
          <div
            ref={observerTarget}
            style={{ height: "20px", margin: "10px 0", textAlign: "center" }}
          >
            {loading && <i className="fas fa-spinner fa-spin text-primary"></i>}
            {!hasMore && tickets.length > 0 && (
              <small className="text-muted">Todos os tickets carregados.</small>
            )}
          </div>
        </>
      )}

      {/* --- ABA 2: BUSCA POR ID --- */}
      {activeTab === "search" && (
        <div className="fade-in mt-4">
          <GenericForm
            fields={searchFields}
            submitText="Buscar Ticket"
            isLoading={loading}
            onSubmit={(data) => fetchTicketById(data.ticketId)}
          />

          {currentTicket && (
            <div className="mt-4 border-t pt-4">
              <h4 className="mb-3">Resultado da Busca:</h4>
              {/* Reutilizamos a tabela para exibir o único item encontrado, 
                   garantindo consistência visual */}
              <Table<SupportTicket>
                data={[currentTicket]}
                columns={columns}
                keyExtractor={(item) => item.id}
              />
            </div>
          )}
        </div>
      )}
    </div>
  );
};
