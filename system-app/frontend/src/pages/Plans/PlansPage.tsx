import React, { useState, useCallback } from 'react';

import styles from './PlansPage.module.scss';
import { PlanForm } from '../../components/MercadoPago/Plans/components/PlanForm';
import { PlanList } from '../../components/MercadoPago/Plans/components/PlanList';
import type { PlanEditDetail } from '../../components/MercadoPago/Plans/types/plans.type';
import { Sidebar } from '../../components/SideBar/components/Sidebar';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';
import { usePlans } from '../../components/MercadoPago/Plans/hooks/usePlans';


// Menu da Sidebar
const MENU_ITEMS: SidebarItem[] = [
    { id: 'list', label: 'Listar Planos', icon: 'fas fa-list' },
    { id: 'create', label: 'Novo Plano', icon: 'fas fa-plus-circle' }
];

export const PlansPage: React.FC = () => {
    // Estado da View Atual
    const [activeView, setActiveView] = useState<string>('list');

    // Estado para Edição (Dados do plano selecionado)
    const [editingPlan, setEditingPlan] = useState<PlanEditDetail | null>(null);

    // Hook de lógica (precisamos do getPlanById aqui para preparar a edição)
    const { getPlanById } = usePlans();

    // --- Handlers de Navegação ---

    const handleSidebarClick = (item: SidebarItem) => {
        if (item.id === 'create') {
            // Se clicar em criar, limpamos qualquer edição pendente
            setEditingPlan(null);
        }
        setActiveView(item.id.toString());
    };

    const handleEditRequest = useCallback(async (id: string) => {
        // 1. Busca os dados completos do plano
        const planDetail = await getPlanById(id);

        if (planDetail) {
            setEditingPlan(planDetail);
            setActiveView('create'); // Reutilizamos a view de form ('create') para edição
        }
    }, [getPlanById]);

    const handleFormSuccess = () => {
        // Ao salvar com sucesso, volta para a lista e limpa edição
        setEditingPlan(null);
        setActiveView('list');
    };

    const handleFormCancel = () => {
        setEditingPlan(null);
        setActiveView('list');
    };

    // --- Renderização Condicional do Conteúdo ---

    const renderContent = () => {
        switch (activeView) {
            case 'list':
                return (
                    <>
                        <div className={styles.pageHeader}>
                            <h2>Gerenciar Planos</h2>
                            <p>Visualize, edite ou exclua os planos de assinatura disponíveis.</p>
                        </div>
                        <PlanList onEditRequest={handleEditRequest} />
                    </>
                );

            case 'create':
                return (
                    <PlanForm
                        initialData={editingPlan}
                        onSuccess={handleFormSuccess}
                        onCancel={handleFormCancel}
                    />
                );

            default:
                return null;
        }
    };

    return (
        <div className={styles.pageContainer}>
            {/* Sidebar Component  */}
            <Sidebar
                items={MENU_ITEMS}
                activeItemId={activeView} //  Passamos o ID ativo para highlight
                onItemClick={handleSidebarClick} // [cite: 4] Navegação
                logo={
                    // Exemplo de Logo Simples
                    <div style={{ padding: '10px', fontWeight: 'bold', fontSize: '1.2rem', color: '#fff' }}>
                        <i className="fas fa-cubes"></i> Admin
                    </div>
                }
            >
                {/* Children da Sidebar (Footer) [cite: 11] */}
                <div style={{ padding: '1rem', color: '#aaa', fontSize: '0.8rem', textAlign: 'center' }}>
                    © 2024 Sistema v1.0
                </div>
            </Sidebar>

            {/* Conteúdo Principal */}
            <main className={styles.contentArea}>
                {renderContent()}
            </main>
        </div>
    );
};