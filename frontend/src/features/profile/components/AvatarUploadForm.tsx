import React from 'react';

import styles from './AvatarUploadForm.module.scss';
import { type FormField, GenericForm } from '../../../components/Form/GenericForm';
import { useAvatarUpdate } from '../hooks/useAvatarUpdate';

interface AvatarFormSchema {
  file: FileList;
}

export const AvatarUploadForm: React.FC = () => {
  const { updateAvatar, isLoading } = useAvatarUpdate();

  const fields: FormField<AvatarFormSchema>[] = [
    {
      name: 'file',
      label: 'Selecione uma nova foto',
      type: 'file', // Tipo suportado pelo seu GenericForm [cite: 7, 26]
      accept: 'image/png, image/jpeg, image/jpg', // Restrição de tipo [cite: 27]
      validation: {
        required: 'A imagem é obrigatória' // Validação simples
      }
    }
  ];

  return (
    <div className={styles.container}>
      <p className={styles.helperText}>
        Formatos aceitos: JPG, PNG. Tamanho máximo: 5MB.
      </p>
      
      <GenericForm<AvatarFormSchema>
        fields={fields}
        onSubmit={updateAvatar}
        submitText="Atualizar Foto"
        isLoading={isLoading}
      />
    </div>
  );
};