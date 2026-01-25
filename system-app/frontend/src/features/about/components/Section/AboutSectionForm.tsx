import React from "react";
// CORREÇÃO 1: Removido 'type' da importação do GenericForm (ele é um componente/valor)

import styles from "../../styles/AboutForm.module.scss";

import {
  type FormField,
  GenericForm,
} from "../../../../components/Form/GenericForm";
import type {
  AboutSectionData,
  AboutSectionFormValues,
} from "../../types/about.types";

interface SectionFormProps {
  initialData?: AboutSectionData;
  onSubmit: (data: AboutSectionFormValues) => void;
  isLoading?: boolean;
}

export const AboutSectionForm: React.FC<SectionFormProps> = ({
  initialData,
  onSubmit,
  isLoading,
}) => {
  const isEditing = !!initialData;

  const fields: FormField<AboutSectionFormValues>[] = [
    {
      name: "title",
      label: "Título Principal",
      type: "text",
      placeholder: "Ex: Nossa História",
      validation: { required: "O título é obrigatório" },
      colSpan: 12,
    },
    {
      name: "description",
      label: "Conteúdo da Seção",
      type: "textarea",
      placeholder: "Escreva sobre a empresa...",
      validation: { required: "A descrição é obrigatória" },
      colSpan: 12,
    },
    {
      name: "newImage",
      label: isEditing ? "Alterar Imagem (Opcional)" : "Imagem de Destaque",
      type: "file",
      accept: "image/*",
      validation: { required: isEditing ? false : "A imagem é obrigatória" },
      colSpan: 6,
    },
    {
      name: "imageAlt",
      label: "Descrição da Imagem (Alt Text)",
      type: "text",
      placeholder: "Descreva a imagem para acessibilidade",
      validation: { required: "O texto alternativo é obrigatório para SEO" },
      colSpan: 6,
    },
  ];

  // CORREÇÃO 2: Substituído 'any' por 'Partial<AboutSectionFormValues>'
  const defaultValues: Partial<AboutSectionFormValues> = initialData
    ? {
        title: initialData.title,
        description: initialData.description,
        imageAlt: initialData.imageAlt,
      }
    : {};

  return (
    <div className={styles.formContainer}>
      <div className={styles.header}>
        <h2>{isEditing ? "Editar Seção" : "Nova Seção Sobre"}</h2>
        <p>Gerencie o conteúdo principal da página About.</p>
      </div>

      {isEditing && initialData?.imageUrl && (
        <div className={`${styles.imagePreviewHint} ${styles.warning}`}>
          <i className="fas fa-info-circle"></i>
          <span>
            Imagem atual configurada. Envie um arquivo apenas para substituí-la.
          </span>
        </div>
      )}

      <GenericForm<AboutSectionFormValues>
        fields={fields}
        onSubmit={onSubmit}
        defaultValues={defaultValues}
        isLoading={isLoading}
        submitText={isEditing ? "Salvar Alterações" : "Criar Seção"}
      />
    </div>
  );
};
