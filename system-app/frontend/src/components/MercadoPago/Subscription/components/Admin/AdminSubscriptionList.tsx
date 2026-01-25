import React, { useState } from "react";

import styles from "../../styles/AdminSubscriptionList.module.scss";
import { useAdminSubscription } from "../../hooks/useAdminSubscription";
import { type TableColumn, Table } from "../../../../Table/Table";
import type { AdminSubscriptionDetail } from "../../types/adminSubscription.type";

export const AdminSubscriptionList: React.FC = () => {
  // Estado local para o input de busca
  const [queryInput, setQueryInput] = useState("");

  // Hook customizado
  const {
    subscription,
    loading,
    searchSubscription,
    // error pode ser tratado exibindo um Toast ou alert
  } = useAdminSubscription();

  // --- HELPERS DE FORMATAÇÃO (UX) ---

  const formatCurrency = (val?: number, currencyId = "BRL") => {
    if (val === undefined || val === null) return "-";
    return new Intl.NumberFormat("pt-BR", {
      style: "currency",
      currency: currencyId,
    }).format(val);
  };

  const formatDate = (dateStr: string) => {
    if (!dateStr) return "-";
    return new Intl.DateTimeFormat("pt-BR").format(new Date(dateStr));
  };

  const getStatusClass = (status: string) => {
    switch (status) {
      case "authorized":
        return styles.authorized;
      case "paused":
        return styles.paused;
      case "cancelled":
        return styles.cancelled;
      default:
        return styles.default;
    }
  };

  // --- DEFINIÇÃO DAS COLUNAS DA TABELA ---

  const columns: TableColumn<AdminSubscriptionDetail>[] = [
    {
      header: "ID / Email",
      width: "25%",
      render: (item) => (
        <div>
          <div style={{ fontWeight: "bold", fontSize: "0.9em" }}>{item.id}</div>
          <div style={{ color: "#666", fontSize: "0.85em" }}>
            {item.payer_email || "Email não inf."}
          </div>
        </div>
      ),
    },
    {
      header: "Status",
      width: "15%",
      render: (item) => (
        <span
          className={`${styles.statusBadge} ${getStatusClass(item.status)}`}
        >
          {item.status}
        </span>
      ),
    },
    {
      header: "Valor",
      width: "15%",
      render: (item) =>
        formatCurrency(
          item.auto_recurring?.transaction_amount,
          item.auto_recurring?.currency_id || "BRL"
        ),
    },
    {
      header: "Próx. Pagamento",
      width: "15%",
      render: (item) =>
        item.next_payment_date ? formatDate(item.next_payment_date) : "N/A",
    },
    {
      header: "Criado em",
      width: "15%",
      render: (item) => formatDate(item.date_created),
    },
    {
      header: "Ações",
      width: "15%",
      render: (item) => (
        <div className={styles.actions}>
          <button onClick={() => console.log("Editar", item.id)}>
            Gerenciar
          </button>
        </div>
      ),
    },
  ];

  // --- HANDLERS ---

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (queryInput.trim()) {
      searchSubscription(queryInput);
    }
  };

  // Transformação: A Table espera um array, mas a busca retorna 1 item ou null.
  // Criamos um array contendo o item (se existir) para passar para a Table.
  const tableData = subscription ? [subscription] : [];

  return (
    <div className={styles.container}>
      <h2>Gerenciamento de Assinaturas</h2>

      {/* Barra de Busca */}
      <form onSubmit={handleSearch} className={styles.searchSection}>
        <input
          type="text"
          placeholder="Cole o ID da assinatura ou Email..."
          value={queryInput}
          onChange={(e) => setQueryInput(e.target.value)}
        />
        <button type="submit" disabled={loading}>
          {loading ? (
            <>Buscando...</>
          ) : (
            <>
              {/* Você pode usar um ícone aqui, ex: FontAwesome */}
              <i className="fas fa-search" style={{ marginRight: 5 }}></i>{" "}
              Buscar
            </>
          )}
        </button>
      </form>

      {/* Tabela Genérica */}
      <Table<AdminSubscriptionDetail>
        data={tableData}
        columns={columns}
        isLoading={loading}
        keyExtractor={(item) => item.id}
        emptyMessage="Nenhuma assinatura encontrada. Utilize a busca acima."
      />
    </div>
  );
};
