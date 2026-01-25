import React from 'react';
import styles from '../../styles/HomeForms.module.scss';
import { type FormField, GenericForm } from '../../../../components/Form/GenericForm';
import type { ServiceData, ServiceFormValues } from '../../types/home.types'; //

interface ServiceFormProps {
  initialData?: ServiceData;
  onSubmit: (data: ServiceFormValues) => void;
  isLoading?: boolean;
}

export const ServiceForm: React.FC<ServiceFormProps> = ({ 
  initialData, 
  onSubmit, 
  isLoading 
}) => {
  
  const isEditing = !!initialData;

  const fields: FormField<ServiceFormValues>[] = [
    {
      name: 'title',
      label: 'Título do Serviço',
      type: 'text',
      placeholder: 'Ex: Desenvolvimento Web',
      validation: { required: 'O título é obrigatório' },
      colSpan: 6
    },
    {
      name: 'iconClass',
      label: 'Classe do Ícone (FontAwesome)',
      type: 'text',
      placeholder: 'Ex: fas fa-code',
      validation: { required: 'O ícone é obrigatório' },
      colSpan: 6
    },
    {
      name: 'description',
      label: 'Descrição Curta',
      type: 'textarea',
      placeholder: 'Breve resumo do serviço oferecido...',
      validation: { 
        required: 'A descrição é obrigatória',
        maxLength: { value: 200, message: 'Máximo de 200 caracteres' }
      },
      colSpan: 12
    },
    {
      name: 'actionText',
      label: 'Texto do Link',
      type: 'text',
      placeholder: 'Ex: Ver Detalhes',
      validation: { required: 'O texto do link é obrigatório' },
      colSpan: 6
    },
    {
      name: 'actionUrl',
      label: 'URL de Destino',
      type: 'text',
      placeholder: 'Ex: /servicos/dev',
      validation: { required: 'A URL é obrigatória' },
      colSpan: 6
    }
  ];

  // CORREÇÃO: Substituído 'any' por 'Partial<ServiceFormValues>'
  const defaultValues: Partial<ServiceFormValues> = initialData ? {
    title: initialData.title,
    iconClass: initialData.iconClass,
    description: initialData.description,
    actionText: initialData.actionText,
    actionUrl: initialData.actionUrl
  } : {};

  return (
    <div className={styles.formContainer}>
      <div className={styles.header}>
        <h3>{isEditing ? 'Editar Serviço' : 'Novo Card de Serviço'}</h3>
        <p>Gerencie os serviços exibidos na página inicial.</p>
      </div>

      <div className={styles.iconHint}>
        <i className="fas fa-info-circle"></i> Dica: Utilize classes do 
        <a href="https://fontawesome.com/icons" target="_blank" rel="noreferrer"> FontAwesome </a> 
        (ex: <code>fas fa-rocket</code>).
      </div>

      <GenericForm<ServiceFormValues>
        fields={fields}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        isLoading={isLoading}
        submitText={isEditing ? "Salvar Serviço" : "Criar Serviço"}
      />
    </div>
  );
};