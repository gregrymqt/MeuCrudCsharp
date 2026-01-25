import React, { useMemo } from 'react';

import styles from '../styles/WalletList.module.scss';
import { ActionMenu } from '../../../ActionMenu/ActionMenu';
import { type TableColumn, Table } from '../../../Table/Table';
import { useWallet } from '../hooks/useWallet';
import type { WalletCard } from '../types/wallet.type';

export const WalletList: React.FC = () => {
  const { cards, loading, removeCard } = useWallet();

  // Definição das colunas da Tabela
  const columns: TableColumn<WalletCard>[] = useMemo(() => [
    {
      header: 'Cartão',
      width: '25%',
      render: (card) => (
        <div className={styles.brandInfo}>
          {/* Ícone simples ou texto da bandeira */}
          <span>{card.paymentMethodId}</span>
          <span style={{ color: '#aaa' }}>••••</span>
          <span>{card.lastFourDigits}</span>
        </div>
      )
    },
    {
      header: 'Validade',
      width: '20%',
      render: (card) => (
        <span>{String(card.expirationMonth).padStart(2, '0')}/{card.expirationYear}</span>
      )
    },
    {
      header: 'Status',
      width: '30%',
      render: (card) => (
        <span className={`${styles.statusBadge} ${card.isSubscriptionActiveCard ? styles.active : styles.secondary}`}>
          {card.isSubscriptionActiveCard ? 'Principal (Assinatura)' : 'Carteira'}
        </span>
      )
    },
    {
      header: 'Ações',
      width: '10%',
      render: (card) => (
        <ActionMenu
          // Como é um CRD, a edição não foi solicitada, mas o componente exige a prop.
          // Podemos deixar um console.log ou futuramente implementar "Tornar Principal" aqui.
          onEdit={() => console.log('Feature futura: Tornar cartão principal', card.id)}
          
          // A lógica de bloqueio de deleção já está dentro do removeCard (useWallet),
          // então podemos chamar direto.
          onDelete={() => removeCard(card.id)}
          
          // Opcional: Se quiser desabilitar o menu visualmente quando for o principal
          // disabled={card.isSubscriptionActiveCard} 
        />
      )
    }
  ], [removeCard]);

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h3>Meus Cartões</h3>
        <span className={styles.cardCount}>
          {cards.length} {cards.length === 1 ? 'cartão salvo' : 'cartões salvos'}
        </span>
      </div>

      <Table<WalletCard>
        data={cards}
        columns={columns}
        keyExtractor={(item) => item.id}
        isLoading={loading}
        emptyMessage="Você ainda não possui cartões salvos."
      />
    </div>
  );
};