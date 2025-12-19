import React, { useMemo } from 'react';
import { type FormField, GenericForm } from '../../../../../Form/GenericForm';
import type { PixPayer, IdentificationType } from '../types/pix.types';


interface PixFormProps {
  onSubmit: (data: PixPayer) => void;
  isLoading: boolean;
  docTypes: IdentificationType[];
  defaultEmail?: string;
  defaultName?: string;
}

export const PixForm: React.FC<PixFormProps> = ({ 
  onSubmit, 
  isLoading, 
  docTypes,
  defaultEmail,
  defaultName
}) => {
  
  // Construção dinâmica dos campos
  const fields = useMemo<FormField<PixPayer>[]>(() => [
    {
      name: 'firstName',
      label: 'Nome',
      type: 'text',
      colSpan: 6,
      validation: { required: 'Nome é obrigatório' }
    },
    {
      name: 'lastName',
      label: 'Sobrenome',
      type: 'text',
      colSpan: 6,
      validation: { required: 'Sobrenome é obrigatório' }
    },
    {
      name: 'email',
      label: 'E-mail',
      type: 'email',
      colSpan: 12,
      validation: { 
        required: 'E-mail é obrigatório',
        pattern: { value: /^\S+@\S+$/i, message: 'E-mail inválido' }
      }
    },
    {
      name: 'identificationType',
      label: 'Tipo de Documento',
      type: 'select',
      colSpan: 4,
      options: docTypes.map(d => ({ label: d.name, value: d.id })),
      validation: { required: 'Selecione o tipo' }
    },
    {
      name: 'identificationNumber',
      label: 'Número do Documento',
      type: 'text',
      colSpan: 8,
      validation: { required: 'Documento é obrigatório' }
    }
  ], [docTypes]);

  // Separa o nome completo se vier nos defaults
  const defaultValues = useMemo(() => {
    return {
      email: defaultEmail || '',
      firstName: defaultName?.split(' ')[0] || '',
      // Tenta pegar o resto do nome, ou deixa vazio
      lastName: defaultName?.split(' ').slice(1).join(' ') || ''
    } as any;
  }, [defaultEmail, defaultName]);

  return (
    <GenericForm<PixPayer>
      fields={fields}
      onSubmit={onSubmit}
      isLoading={isLoading}
      submitText="Gerar QR Code PIX"
      defaultValues={defaultValues}
    />
  );
};