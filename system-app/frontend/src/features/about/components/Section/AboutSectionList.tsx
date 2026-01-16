import React from "react";

import styles from "./AboutLists.module.scss";
import { ActionMenu } from "../../../../components/ActionMenu/ActionMenu";
import { type TableColumn, Table } from "../../../../components/Table/Table";
import type { AboutSectionData } from "../../types/about.types";

interface SectionListProps {
  data: AboutSectionData[];
  isLoading: boolean;
  onEdit: (section: AboutSectionData) => void;
  onDelete: (id: number) => void;
}

export const AboutSectionList: React.FC<SectionListProps> = ({
  data,
  isLoading,
  onEdit,
  onDelete,
}) => {
  // Definição das colunas da tabela
  const columns: TableColumn<AboutSectionData>[] = [
    {
      header: "Imagem",
      width: "80px",
      render: (item) => (
        <img
          src={item.imageUrl}
          alt={item.imageAlt || item.title}
          className={styles.sectionThumbnail}
        />
      ),
    },
    {
      header: "Título",
      accessor: "title",
      width: "25%",
    },
    {
      header: "Descrição",
      render: (item) => (
        // Truncar texto longo para não quebrar a tabela
        <span title={item.description}>
          {item.description.length > 60
            ? `${item.description.substring(0, 60)}...`
            : item.description}
        </span>
      ),
    },
    {
      header: "Ações",
      width: "100px",
      render: (item) => (
        <div className={styles.actionsCell}>
          <ActionMenu
            onEdit={() => onEdit(item)}
            onDelete={() => onDelete(item.id)}
          />
        </div>
      ),
    },
  ];

  return (
    <div className={styles.listContainer}>
      <Table<AboutSectionData>
        data={data}
        columns={columns}
        isLoading={isLoading}
        keyExtractor={(item) => item.id}
        emptyMessage="Nenhuma seção cadastrada."
      />
    </div>
  );
};
