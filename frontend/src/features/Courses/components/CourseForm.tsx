import React from 'react';
import type { CourseFormData, CourseFormProps } from '../types/course-manager.types';
import styles from '../styles/CourseForm.module.scss';
import { type FormField, GenericForm } from '../../../../components/Form/GenericForm';

export const CourseForm: React.FC<CourseFormProps> = ({ 
  initialData, 
  isLoading, 
  onSubmit, 
  onCancel 
}) => {
  
  // Configuração dos Campos para o GenericForm
  const fields: FormField<CourseFormData>[] = [
    {
      name: 'name',
      label: 'Nome do Curso',
      type: 'text',
      placeholder: 'Ex: Introdução ao React',
      validation: { 
        required: 'O nome do curso é obrigatório',
        minLength: { value: 3, message: 'Mínimo de 3 caracteres' }
      },
      colSpan: 12
    },
    {
      name: 'description',
      label: 'Descrição Detalhada',
      type: 'textarea', // Usando o tipo textarea do seu GenericForm [cite: 19]
      placeholder: 'Descreva o conteúdo do curso...',
      validation: { required: 'A descrição é obrigatória' },
      colSpan: 12
    },
    // Exemplo de campo de arquivo conforme sua atualização no GenericForm [cite: 26]
    {
      name: 'thumbnail',
      label: 'Imagem de Capa (Opcional)',
      type: 'file',
      accept: 'image/*',
      colSpan: 12
    }
  ];

  // Valores padrão se for edição
  const defaultValues = initialData ? {
    name: initialData.name,
    description: initialData.description
  } : {};

  return (
    <div className={styles.formContainer}>
      <div className={styles.formHeader}>
        <h2>
          {initialData ? 'Editar Curso' : 'Novo Curso'}
          {initialData && <span>Editando: {initialData.name}</span>}
        </h2>
      </div>

      <GenericForm<CourseFormData>
        fields={fields}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        submitText={initialData ? 'Atualizar Curso' : 'Criar Curso'}
        isLoading={isLoading}
      >
        {/* Botão de Cancelar inserido via children no GenericForm */}
        <div className={styles.actions}>
          <button 
            type="button" 
            className={styles.cancelBtn} 
            onClick={onCancel}
            disabled={isLoading}
          >
            Cancelar
          </button>
        </div>
      </GenericForm>
    </div>
  );
};