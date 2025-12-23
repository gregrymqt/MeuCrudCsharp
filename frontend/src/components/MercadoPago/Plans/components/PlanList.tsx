import React, { useEffect, useState, useMemo } from 'react';

import { ActionMenu } from '../../../ActionMenu/ActionMenu';
import { type TableColumn, Table } from '../../../Table/Table';
import { usePlans } from '../hooks/usePlans';
import type { PlanSummary, PlanEditDetail } from '../types/plans.type';
import styles from '../styles/PlanList.module.scss';

interface PlanListProps {
    onEditRequest: (id: string) => void; // Callback para o pai abrir o Form
}

export const PlanList: React.FC<PlanListProps> = ({ onEditRequest }) => {
    // Hooks e Estados
    const {
        plans,
        pagination,
        loading,
        fetchPlans,
        getPlanById,
        deletePlan,
        currentPlan
    } = usePlans();

    const [activeTab, setActiveTab] = useState<'list' | 'search'>('list');
    const [searchId, setSearchId] = useState('');

    // Carrega a listagem inicial ao entrar na aba 'list'
    useEffect(() => {
        if (activeTab === 'list') {
            fetchPlans(1, 10); // Página 1, 10 itens por padrão
        }
    }, [activeTab, fetchPlans]);

    // --- Handlers ---

    const handlePageChange = (newPage: number) => {
        if (pagination && newPage >= 1 && newPage <= pagination.totalPages) {
            fetchPlans(newPage, pagination.pageSize);
        }
    };

    const handleSearchById = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!searchId.trim()) return;
        await getPlanById(searchId);
    };

    // --- Colunas da Tabela ---

    // Colunas para a Listagem Geral (Pagination)
    const listColumns: TableColumn<PlanSummary>[] = useMemo(() => [
        { header: 'Nome', accessor: 'name', width: '30%' },
        { header: 'Preço', accessor: 'priceDisplay', width: '20%' }, // O backend já manda formatado
        {
            header: 'Recomendado',
            width: '15%',
            render: (item) => (
                <span style={{ color: item.isRecommended ? 'green' : '#ccc' }}>
                    {item.isRecommended ? '★ Sim' : 'Não'}
                </span>
            )
        },
        {
            header: 'Status',
            width: '15%',
            render: (item) => (
                <span className={`badge ${item.isActive ? 'active' : 'inactive'}`}>
                    {item.isActive ? 'Ativo' : 'Inativo'}
                </span>
            )
        },
        {
            header: 'Ações',
            width: '100px',
            render: (item) => (
                <ActionMenu
                    onEdit={() => onEditRequest(item.publicId)}
                    onDelete={() => deletePlan(item.publicId).then(success => {
                        if (success) fetchPlans(pagination?.currentPage || 1); // Recarrega após deletar
                    })}
                />
            )
        }
    ], [pagination, deletePlan, fetchPlans, onEditRequest]);

    // Colunas para a Busca por ID (Detalhe Único)
    // Nota: O tipo PlanEditDetail é diferente do PlanSummary, precisamos adaptar
    const detailColumns: TableColumn<PlanEditDetail>[] = useMemo(() => [
        { header: 'ID Público', accessor: 'publicId', width: '30%' },
        { header: 'Nome', accessor: 'name', width: '25%' },
        {
            header: 'Valor',
            width: '20%',
            // Formata manualmente pois o EditDTO traz 'number'
            render: (item) => new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(item.transactionAmount)
        },
        {
            header: 'Recorrência',
            width: '15%',
            render: (item) => `${item.frequency} ${item.frequencyType === 'months' ? 'Mês(es)' : 'Dia(s)'}`
        },
        {
            header: 'Ações',
            width: '100px',
            render: (item) => (
                <ActionMenu
                    onEdit={() => onEditRequest(item.publicId)}
                    onDelete={() => deletePlan(item.publicId).then(success => {
                        if (success) setSearchId(''); // Limpa busca se deletou o item atual
                    })}
                />
            )
        }
    ], [deletePlan, onEditRequest]);

    // --- Render ---

    return (
        <div className={styles.container}>
            {/* 1. Abas de Navegação */}
            <div className={styles.tabsHeader}>
                <button
                    className={activeTab === 'list' ? styles.active : ''}
                    onClick={() => setActiveTab('list')}
                >
                    Listar Todos
                </button>
                <button
                    className={activeTab === 'search' ? styles.active : ''}
                    onClick={() => setActiveTab('search')}
                >
                    Buscar por ID
                </button>
            </div>

            {/* 2. Conteúdo das Abas */}

            {/* ABA: LISTAGEM PAGINADA */}
            {activeTab === 'list' && (
                <>
                    <Table<PlanSummary>
                        data={plans}
                        columns={listColumns}
                        isLoading={loading}
                        keyExtractor={(item) => item.publicId}
                        emptyMessage="Nenhum plano cadastrado."
                    />

                    {/* Rodapé de Paginação */}
                    {pagination && (
                        <div className={styles.paginationControls}>
                            <span>
                                Página <strong>{pagination.currentPage}</strong> de {pagination.totalPages}
                                (Total: {pagination.totalCount})
                            </span>
                            <div>
                                <button
                                    disabled={!pagination.hasPreviousPage || loading}
                                    onClick={() => handlePageChange(pagination.currentPage - 1)}
                                >
                                    &lt; Anterior
                                </button>
                                <button
                                    disabled={!pagination.hasNextPage || loading}
                                    onClick={() => handlePageChange(pagination.currentPage + 1)}
                                    style={{ marginLeft: '0.5rem' }}
                                >
                                    Próxima &gt;
                                </button>
                            </div>
                        </div>
                    )}
                </>
            )}

            {/* ABA: BUSCA POR ID */}
            {activeTab === 'search' && (
                <>
                    <form className={styles.searchBar} onSubmit={handleSearchById}>
                        <input
                            type="text"
                            placeholder="Cole o ID do plano aqui (UUID)..."
                            value={searchId}
                            onChange={(e) => setSearchId(e.target.value)}
                        />
                        <button type="submit" disabled={loading || !searchId}>
                            {loading ? 'Buscando...' : 'Pesquisar'}
                        </button>
                    </form>

                    {/* Exibimos o currentPlan dentro da Tabela (array de 1 item) */}
                    <Table<PlanEditDetail>
                        data={currentPlan ? [currentPlan] : []}
                        columns={detailColumns}
                        isLoading={loading}
                        keyExtractor={(item) => item.publicId}
                        emptyMessage={searchId ? "Nenhum plano encontrado com este ID." : "Aguardando busca..."}
                    />
                </>
            )}
        </div>
    );
};