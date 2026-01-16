import React from 'react';
// CORREÇÃO 1: Importação correta (GenericForm como valor)

import styles from './AboutForms.module.scss'; // Mesmo arquivo de estilo
import { type FormField, GenericForm } from '../../../../components/Form/GenericForm';
import type { TeamMember, TeamMemberFormValues } from '../../types/about.types';

interface MemberFormProps {
  initialData?: TeamMember;
  onSubmit: (data: TeamMemberFormValues) => void;
  isLoading?: boolean;
}

export const TeamMemberForm: React.FC<MemberFormProps> = ({ 
  initialData, 
  onSubmit, 
  isLoading 
}) => {

  const isEditing = !!initialData;

  const fields: FormField<TeamMemberFormValues>[] = [
    {
      name: 'name',
      label: 'Nome do Colaborador',
      type: 'text',
      placeholder: 'Ex: Ana Silva',
      validation: { required: 'O nome é obrigatório' },
      colSpan: 6
    },
    {
      name: 'role',
      label: 'Cargo / Função',
      type: 'text',
      placeholder: 'Ex: Tech Lead',
      validation: { required: 'O cargo é obrigatório' },
      colSpan: 6
    },
    {
      name: 'newPhoto',
      label: isEditing ? 'Nova Foto (Opcional)' : 'Foto de Perfil',
      type: 'file',
      accept: 'image/*',
      validation: { required: isEditing ? false : 'A foto é obrigatória' },
      colSpan: 12
    },
    {
      name: 'linkedinUrl',
      label: 'LinkedIn (URL)',
      type: 'text',
      placeholder: 'https://linkedin.com/in/...',
      colSpan: 6
    },
    {
      name: 'githubUrl',
      label: 'GitHub (URL)',
      type: 'text',
      placeholder: 'https://github.com/...',
      colSpan: 6
    }
  ];

  // CORREÇÃO 2: Substituído 'any' por 'Partial<TeamMemberFormValues>'
  const defaultValues: Partial<TeamMemberFormValues> = initialData ? {
    name: initialData.name,
    role: initialData.role,
    linkedinUrl: initialData.linkedinUrl,
    githubUrl: initialData.githubUrl
  } : {};

  return (
    <div className={styles.formContainer}>
      <div className={styles.header}>
        <h2>{isEditing ? 'Editar Membro' : 'Adicionar ao Time'}</h2>
        <p>Cadastre ou atualize as informações do colaborador.</p>
      </div>

      <GenericForm<TeamMemberFormValues>
        fields={fields}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        isLoading={isLoading}
        submitText={isEditing ? "Atualizar Membro" : "Adicionar Membro"}
      />
    </div>
  );
};