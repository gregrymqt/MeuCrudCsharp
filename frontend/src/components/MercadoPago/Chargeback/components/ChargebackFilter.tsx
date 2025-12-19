import { type FormField, GenericForm } from '../../../Form/GenericForm';
import styles from '../styles/ChargebackFilter.module.scss';

export interface FilterFormData {
  searchTerm: string;
  status: string;
}

interface Props {
  onFilter: (data: FilterFormData) => void;
  isLoading: boolean;
}

export const ChargebackFilter = ({ onFilter, isLoading }: Props) => {
  
  const fields: FormField<FilterFormData>[] = [
    {
      name: 'searchTerm',
      label: 'Buscar',
      type: 'text',
      placeholder: 'ID, Cliente ou Valor...',
      colSpan: 12 // Mobile: 12 colunas
    },
    {
      name: 'status',
      label: 'Status',
      type: 'select',
      options: [
        { label: 'Todos', value: '' },
        { label: 'Novo', value: 'Novo' },
        { label: 'Aguardando Evidências', value: 'AguardandoEvidencias' },
        { label: 'Em Análise', value: 'EmAnalise' },
        { label: 'Finalizado', value: 'Finalizado' }
      ],
      colSpan: 12 // Mobile: 12 colunas
    }
  ];

  // Ajuste de colunas para Desktop via props do GenericForm não é direto,
  // mas o GenericForm já tem a classe 'col-span-X'. 
  // Se quiser layout lado-a-lado no desktop, altere o colSpan acima dinamicamente ou via CSS.
  // Pelo seu GenericForm, ele usa classes fixas. Vamos assumir que no desktop queremos:
  // Search (6), Status (4), Botão (auto).
  
  // Hack de layout responsivo: Alteramos o objeto fields se necessário ou confiamos no CSS Grid.
  // Vamos manter 12/12 no mobile. Para desktop, o CSS Grid do GenericForm faria o trabalho se passássemos colSpan menor.
  // Vamos ajustar os fields para desktop:
  fields[0].colSpan = 6; 
  fields[1].colSpan = 4;

  return (
    <div className={styles['chargeback-filter']}>
      <GenericForm<FilterFormData>
        fields={fields}
        onSubmit={onFilter}
        submitText="Filtrar"
        isLoading={isLoading}
      />
    </div>
  );
};