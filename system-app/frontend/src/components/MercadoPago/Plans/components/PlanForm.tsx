import React, { useMemo } from 'react';
import { type SubmitHandler } from 'react-hook-form';

import styles from '../styles/PlanForm.module.scss';
import { type FormField, GenericForm } from '../../../Form/GenericForm';
import { usePlans } from '../hooks/usePlans';
import type { PlanEditDetail, UpdatePlanRequest, CreatePlanRequest } from '../types/plans.type';

// Interface "Plana" para controlar o formulário visualmente
interface PlanFormSchema {
    reason: string;
    description: string;
    transactionAmount: number;
    frequency: number;
    frequencyType: string;
}

interface PlanFormProps {
    initialData?: PlanEditDetail | null; // Se vier preenchido, é Edição
    onSuccess: () => void; // Callback para fechar modal ou atualizar lista
    onCancel?: () => void;
}

export const PlanForm: React.FC<PlanFormProps> = ({ initialData, onSuccess, onCancel }) => {
    const { createPlan, updatePlan, loading } = usePlans();

    const isEditing = !!initialData;

    // 1. Definição dos Campos do Formulário
    const formFields: FormField<PlanFormSchema>[] = [
        {
            name: 'reason',
            label: 'Nome do Plano',
            type: 'text',
            placeholder: 'Ex: Plano Gold Mensal',
            colSpan: 12, // Largura total no mobile e desktop
            validation: { required: 'O nome do plano é obrigatório.' }
        },
        {
            name: 'transactionAmount',
            label: 'Valor (R$)',
            type: 'number',
            placeholder: '0.00',
            colSpan: 6, // Metade da tela
            validation: {
                required: 'Informe o valor.',
                min: { value: 1, message: 'O valor mínimo é 1.' }
            }
        },
        {
            name: 'frequency',
            label: 'Frequência',
            type: 'number',
            placeholder: 'Ex: 1',
            colSpan: 3,
            validation: { required: 'Obrigatório', min: 1 }
        },
        {
            name: 'frequencyType',
            label: 'Tipo',
            type: 'select',
            colSpan: 3,
            options: [
                { label: 'Meses', value: 'months' },
                { label: 'Dias', value: 'days' }
            ],
            validation: { required: 'Selecione' }
        },
        {
            name: 'description',
            label: 'Descrição / Benefícios',
            type: 'textarea',
            placeholder: 'Descreva os detalhes que aparecerão para o cliente...',
            colSpan: 12,
            validation: { required: 'A descrição é obrigatória.' }
        }
    ];

    // 2. Valores Iniciais (Mapeia do DTO do Backend para o Form Schema)
    const defaultValues: Partial<PlanFormSchema> = useMemo(() => {
        if (!initialData) return {
            frequencyType: 'months',
            frequency: 1
        };

        return {
            reason: initialData.name,
            // Agora o TS reconhece .description, removendo o erro da linha 89 "(initialData as any)" [cite: 3]
            description: initialData.description || '',
            transactionAmount: initialData.transactionAmount,
            frequency: initialData.frequency,
            frequencyType: initialData.frequencyType
        };
    }, [initialData]);

    // 3. Função de Submit
    const onSubmit: SubmitHandler<PlanFormSchema> = async (data) => {

        // Monta o objeto aninhado que o Backend/Mercado Pago espera
        const payloadCommon = {
            reason: data.reason,
            description: data.description,
            auto_recurring: {
                frequency: Number(data.frequency),
                frequency_type: data.frequencyType,
                transaction_amount: Number(data.transactionAmount),
                currency_id: 'BRL' // Fixo BRL conforme contexto
            }
        };

        let success = false;

        if (isEditing && initialData) {
            // Lógica de Update
            const updatePayload: UpdatePlanRequest = { ...payloadCommon };
            success = await updatePlan(initialData.publicId, updatePayload);
        } else {
            // Lógica de Create
            const createPayload: CreatePlanRequest = { ...payloadCommon };
            success = await createPlan(createPayload);
        }

        if (success) {
            onSuccess();
        }
    };

    return (
        <div className={styles['plan-form-container']}>
            <div className={styles['plan-form-header']}>
                <h3>{isEditing ? `Editar Plano` : 'Novo Plano de Assinatura'}</h3>
                <p>Preencha as informações para configurar a cobrança no Mercado Pago.</p>
            </div>

            <div className={styles['generic-form']}>
                <GenericForm<PlanFormSchema>
                    fields={formFields}
                    onSubmit={onSubmit}
                    defaultValues={defaultValues}
                    isLoading={loading}
                    submitText={isEditing ? 'Salvar Alterações' : 'Criar Plano'}
                >
                    {/* Botão de Cancelar Injetado como Children (opcional) */}
                    {onCancel && (
                        <button
                            type="button"
                            className={styles['cancel-btn']}
                            onClick={onCancel}
                            disabled={loading}
                        >
                            Cancelar
                        </button>
                    )}
                </GenericForm>
            </div>
        </div>
    );
};