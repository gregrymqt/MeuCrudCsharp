import React from "react";

import styles from "../../styles/HomeLists.module.scss";
import { ActionMenu } from "../../../../components/ActionMenu/ActionMenu";
import { type TableColumn, Table } from "../../../../components/Table/Table";
import type { HeroSlideData } from "../../types/home.types";

interface HeroListProps {
  data: HeroSlideData[];
  isLoading: boolean;
  onEdit: (item: HeroSlideData) => void;
  onDelete: (id: number) => void;
}

export const HeroList: React.FC<HeroListProps> = ({
  data,
  isLoading,
  onEdit,
  onDelete,
}) => {
  const columns: TableColumn<HeroSlideData>[] = [
    {
      header: "Preview",
      width: "100px",
      render: (item) => (
        <img
          src={item.imageUrl}
          alt={item.title}
          className={styles.heroThumbnail}
          // Fallback simples caso a imagem quebre
          onError={(e) => {
            (e.target as HTMLImageElement).src =
              "https://via.placeholder.com/80x45?text=No+Img";
          }}
        />
      ),
    },
    {
      header: "Título",
      accessor: "title",
      width: "25%",
    },
    {
      header: "Subtítulo",
      render: (item) => (
        <span className={styles.truncateText} title={item.subtitle}>
          {item.subtitle}
        </span>
      ),
    },
    {
      header: "Call to Action",
      accessor: "actionText",
      width: "15%",
    },
    {
      header: "Ações",
      width: "80px",
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
      <Table<HeroSlideData>
        data={data}
        columns={columns}
        isLoading={isLoading}
        keyExtractor={(item) => item.id}
        emptyMessage="Nenhum slide cadastrado no Hero."
      />
    </div>
  );
};
