import { useState, useCallback } from 'react';
import { PlanService } from '../services/plans.service';
import { AlertService } from '../../../../shared/services/alert.service';
import type { PlanSummary, PlanEditDetail, PagedResult, CreatePlanRequest, UpdatePlanRequest } from '../types/plans.type';
import { ApiError } from '../../../../shared/services/api.service';


export const usePlans = () => {
    // Estados de Controle
    const [loading, setLoading] = useState(false);
    const [plans, setPlans] = useState<PlanSummary[]>([]);
    const [currentPlan, setCurrentPlan] = useState<PlanEditDetail | null>(null);

    // Estado de Paginação (Para controlar a Grid)
    const [pagination, setPagination] = useState<PagedResult<PlanSummary> | null>(null);

    // --- ACTIONS ---

    /**
     * Busca todos os planos com paginação
     */
    const fetchPlans = useCallback(async (page: number = 1, pageSize: number = 10) => {
        setLoading(true);
        try {
            const data = await PlanService.getAll(page, pageSize);
            setPlans(data.items);
            setPagination(data);
        } catch (error) {
            if (error instanceof ApiError) {
                // Usa o AlertService para feedback visual
                AlertService.error(
                    'Erro ao carregar',
                    error?.message || 'Não foi possível buscar os planos.'
                );
            }
        } finally {
            setLoading(false);
        }
    }, []);

    /**
     * Busca um plano específico pelo ID (Para edição)
     */
    const getPlanById = useCallback(async (id: string) => {
        setLoading(true);
        try {
            const data = await PlanService.getById(id);
            setCurrentPlan(data);
            return data;
        } catch (error) {
            if (error instanceof ApiError) {
                AlertService.error('Erro', error?.message || 'Não foi possível carregar os detalhes do plano.');
            }
            return null;
        } finally {
            setLoading(false);
        }
    }, []);

    /**
     * Cria um novo plano
     */
    const createPlan = async (planData: CreatePlanRequest): Promise<boolean> => {
        setLoading(true);
        try {
            await PlanService.create(planData);
            await AlertService.success('Sucesso!', 'O plano foi criado corretamente.');
            return true;
        } catch (error) {
            if (error instanceof ApiError) {
                // Tratamento de erro vindo do Backend (Validation ou MP)
                const msg = error?.message || 'Erro ao comunicar com Mercado Pago.';
                AlertService.error('Falha ao criar', msg);
            }
            return false;
        } finally {
            setLoading(false);
        }
    };

    /**
     * Atualiza um plano existente
     */
    const updatePlan = async (id: string, planData: UpdatePlanRequest): Promise<boolean> => {
        setLoading(true);
        try {
            await PlanService.update(id, planData);
            await AlertService.success('Atualizado!', 'As alterações foram salvas.');
            return true;
        } catch (error) {
            if (error instanceof ApiError) {
                const msg = error?.message || 'Erro ao atualizar o plano.';
                AlertService.error('Falha na atualização', msg);
            }
            return false;
        } finally {
            setLoading(false);
        }
    };

    /**
     * Remove um plano (Com confirmação visual antes)
     */
    const deletePlan = async (id: string): Promise<boolean> => {
        // 1. Pergunta primeiro (usando seu AlertService.confirm)
        const { isConfirmed } = await AlertService.confirm(
            'Tem certeza?',
            'Esta ação removerá o plano permanentemente e pode afetar assinaturas futuras.'
        );

        if (!isConfirmed) return false;

        // 2. Se confirmou, executa
        setLoading(true);
        try {
            await PlanService.delete(id);
            await AlertService.success('Removido', 'O plano foi excluído com sucesso.');

            // Remove localmente da lista para evitar reload desnecessário (Opcional)
            setPlans((prev) => prev.filter((p) => p.publicId !== id));

            return true;
        } catch (error) {
            if (error instanceof ApiError) {
                AlertService.error('Erro ao excluir', error?.message || 'Verifique se existem assinaturas ativas neste plano.');
            }
            return false;
        } finally {
            setLoading(false);
        }
    };

    // Expor tudo que o componente precisa
    return {
        // Estados
        plans,
        currentPlan,
        pagination,
        loading,

        // Métodos
        fetchPlans,
        getPlanById,
        createPlan,
        updatePlan,
        deletePlan
    };
};