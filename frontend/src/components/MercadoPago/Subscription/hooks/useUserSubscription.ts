import { useState, useEffect, useCallback } from 'react';
import { SubscriptionService } from '../services/UserSubscriptionService'; // Ajuste o caminho
import { AlertService } from '../../../../shared/services/alert.service';
import { ApiError } from '../../../../shared/services/api.service';
import type { SubscriptionDetailsDto } from '../types/userSubscription.type';


export const useSubscription = () => {
    const [subscription, setSubscription] = useState<SubscriptionDetailsDto | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [isProcessing, setIsProcessing] = useState<boolean>(false);

    // Função para buscar os dados (usada no mount e após atualizações)
    const fetchSubscription = useCallback(async () => {
        try {
            setIsLoading(true);
            const data = await SubscriptionService.getDetails();
            setSubscription(data);
        } catch (error) {
            const msg = error instanceof ApiError ? error.message : 'Não foi possível carregar sua assinatura.';
            // Não bloqueamos com AlertService.error aqui para não ser intrusivo no load inicial,
            // mas você pode descomentar se preferir um popup.
            console.error(msg);
        } finally {
            setIsLoading(false);
        }
    }, []);

    // Carrega dados ao montar o componente
    useEffect(() => {
        fetchSubscription();
    }, [fetchSubscription]);

    /**
     * Função genérica para alterar status com confirmação
     */
    const handleStatusChange = async (
        newStatus: 'paused' | 'authorized' | 'cancelled',
        actionName: string,
        confirmMessage: string
    ) => {
        // 1. Confirmação Visual
        const { isConfirmed } = await AlertService.confirm(
            `Deseja ${actionName} a assinatura?`,
            confirmMessage,
            `Sim, ${actionName}`
        );

        if (!isConfirmed) return;

        // 2. Execução da mudança
        try {
            setIsProcessing(true);
            await SubscriptionService.updateStatus(newStatus);

            // 3. Feedback de Sucesso 
            await AlertService.success(
                'Sucesso!',
                `Sua assinatura foi atualizada para: ${actionName}.`
            );

            // 4. Atualiza os dados na tela para refletir o novo status
            await fetchSubscription();

        } catch (error) {
            // 5. Tratamento de Erro 
            const msg = error instanceof ApiError ? error.message : `Erro ao tentar ${actionName} assinatura.`;
            await AlertService.error('Ocorreu um erro', msg);
        } finally {
            setIsProcessing(false);
        }
    };

    // --- Ações Públicas Expostas ---

    const pauseSubscription = () =>
        handleStatusChange(
            'paused',
            'pausar',
            'Sua assinatura será interrompida temporariamente e você perderá acesso aos benefícios até reativar.'
        );

    const reactivateSubscription = () =>
        handleStatusChange(
            'authorized',
            'reativar',
            'Sua assinatura voltará a ser cobrada normalmente no próximo ciclo.'
        );

    const cancelSubscription = () =>
        handleStatusChange(
            'cancelled',
            'cancelar',
            'ATENÇÃO: Isso encerrará sua assinatura permanentemente. Para voltar, você terá que assinar novamente.'
        );

    return {
        subscription,
        isLoading,
        isProcessing,
        refresh: fetchSubscription, // Caso queira um botão de "Recarregar" manual
        actions: {
            pause: pauseSubscription,
            reactivate: reactivateSubscription,
            cancel: cancelSubscription
        }
    };
};