import React from 'react';
import styles from '../../styles/HomeForms.module.scss';
import { type FormField, GenericForm } from '../../../../components/Form/GenericForm';
import type { HeroSlideData, HeroFormValues } from '../../types/home.types'; //

interface HeroFormProps {
  initialData?: HeroSlideData;
  onSubmit: (data: HeroFormValues) => void;
  isLoading?: boolean;
  onCancel?: () => void;
}

export const HeroForm: React.FC<HeroFormProps> = ({ 
  initialData, 
  onSubmit, 
  isLoading 
}) => {
  
  const isEditing = !!initialData;

  const fields: FormField<HeroFormValues>[] = [
    {
      name: 'title',
      label: 'Título Principal',
      type: 'text',
      placeholder: 'Ex: Bem-vindo à Inovação',
      validation: { required: 'O título é obrigatório' },
      colSpan: 6
    },
    {
      name: 'subtitle',
      label: 'Subtítulo',
      type: 'text',
      placeholder: 'Ex: Soluções completas para você',
      validation: { required: 'O subtítulo é obrigatório' },
      colSpan: 6
    },
    {
      name: 'actionText',
      label: 'Texto do Botão (CTA)',
      type: 'text',
      placeholder: 'Ex: Saiba Mais',
      validation: { required: 'O texto do botão é obrigatório' },
      colSpan: 6
    },
    {
      name: 'actionUrl',
      label: 'Link do Botão',
      type: 'text',
      placeholder: 'Ex: /servicos ou https://...',
      validation: { required: 'O link é obrigatório' },
      colSpan: 6
    },
    {
      name: 'newImage',
      label: isEditing ? 'Alterar Imagem (Deixe vazio para manter)' : 'Imagem do Banner',
      type: 'file',
      accept: 'image/png, image/jpeg, image/webp',
      validation: { required: isEditing ? false : 'A imagem é obrigatória' },
      colSpan: 12
    }
  ];

  // CORREÇÃO: Substituído 'any' por 'Partial<HeroFormValues>'
  const defaultValues: Partial<HeroFormValues> = initialData ? {
    title: initialData.title,
    subtitle: initialData.subtitle,
    actionText: initialData.actionText,
    actionUrl: initialData.actionUrl
    // newImage começa undefined na edição, o que é permitido pelo Partial
  } : {};

  return (
    <div className={styles.formContainer}>
      <div className={styles.header}>
        <h3>{isEditing ? 'Editar Slide' : 'Novo Slide Hero'}</h3>
        <p>Configure a imagem e os textos do carrossel principal.</p>
      </div>

      {isEditing && initialData?.imageUrl && (
        <div className={styles.currentImagePreview}>
          <img src={initialData.imageUrl} alt="Atual" />
          <div className="info">
            <strong>Imagem Atual</strong>
            <span>Esta imagem será mantida se você não enviar uma nova.</span>
          </div>
        </div>
      )}

      <GenericForm<HeroFormValues>
        fields={fields}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        isLoading={isLoading}
        submitText={isEditing ? "Salvar Alterações" : "Criar Slide"}
      />
    </div>
  );
};