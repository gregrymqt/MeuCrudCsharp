import React from 'react';
import type { VideoFormData, VideoFormProps } from '../types/video-manager.types';
// Reutiliza estilo do container de form se quiser, ou cria um novo. 
// Usaremos um module específico aqui para manter o padrão.
import styles from '../styles/VideoForm.module.scss'; 
import { type FormField, GenericForm } from 'src/components/Form/GenericForm';

export const VideoForm: React.FC<VideoFormProps> = ({
  initialData,
  courses,
  isLoading,
  onSubmit,
  onCancel
}) => {

  const fields: FormField<VideoFormData>[] = [
    {
      name: 'title',
      label: 'Título do Vídeo',
      type: 'text',
      placeholder: 'Ex: Aula 01 - Instalação',
      validation: { required: 'Título é obrigatório' },
      colSpan: 12
    },
    {
      name: 'courseId',
      label: 'Curso Relacionado',
      type: 'select',
      options: courses.map(c => ({ label: c.name, value: c.publicId })),
      validation: { required: 'Selecione um curso' },
      colSpan: 6
    },
    {
      name: 'duration',
      label: 'Duração (mm:ss)',
      type: 'text',
      placeholder: '00:00',
      colSpan: 6
    },
    {
      name: 'videoUrl',
      label: 'URL do Vídeo (Youtube/Vimeo/Storage)',
      type: 'text',
      placeholder: 'https://...',
      validation: { required: 'A URL do vídeo é obrigatória' },
      colSpan: 12
    },
    {
      name: 'description',
      label: 'Descrição',
      type: 'textarea',
      colSpan: 12
    },
    // Exemplo de campo File caso queira thumbnail
    {
      name: 'thumbnail',
      label: 'Capa do Vídeo (Opcional)',
      type: 'file',
      accept: 'image/*',
      colSpan: 12
    }
  ];

  const defaultValues = initialData ? {
    title: initialData.title,
    description: initialData.description,
    duration: initialData.duration,
    courseId: String(initialData.courseId), // Converter number para string
    videoUrl: initialData.storageIdentifier // Supondo que a URL venha aqui
  } : {};

  return (
    <div className={styles.formWrapper}> {/* Classe definida no CSS similar ao CourseForm */}
      <h2>
        {initialData ? 'Editar Vídeo' : 'Novo Vídeo'}
      </h2>
      
      <GenericForm<VideoFormData>
        fields={fields}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        submitText={initialData ? 'Salvar Alterações' : 'Cadastrar Vídeo'}
        isLoading={isLoading}
      >
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