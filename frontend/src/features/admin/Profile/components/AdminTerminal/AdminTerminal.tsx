import React from 'react';
import { useAdminTerminal } from '../../hooks/useAdminTerminal';
import type { TerminalFormData } from '../../types/adminProfile.types';
import  { Card } from '../../../../components/Card/Card';
import { type FormField, GenericForm } from '../../../../components/Form/GenericForm';

export const AdminTerminal: React.FC = () => {
  const { handleTerminalCommand } = useAdminTerminal();

  // Definição dos campos conforme seu GenericForm exige [cite: 4]
  const terminalFields: FormField<TerminalFormData>[] = [
    {
      name: 'module',
      label: '1. Módulo de Acesso',
      type: 'select', // [cite: 17]
      colSpan: 12,
      validation: { required: 'Selecione um módulo' },
      options: [
        { label: 'Gestão de Cursos (CourseAdmin)', value: 'course' },
        { label: 'Gestão de Vídeos (VideoAdmin)', value: 'video' },
        { label: 'Financeiro (MercadoPago)', value: 'mercadopago' },
      ]
    },
    {
      name: 'action',
      label: '2. Operação Inicial',
      type: 'select',
      colSpan: 6, // Divide a linha
      validation: { required: 'Selecione a ação' },
      options: [
        { label: 'Dashboard / Visão Geral', value: 'dashboard' },
        { label: 'Listagem de Dados', value: 'list' },
        { label: 'Cadastrar Novo', value: 'create' },
      ]
    },
    {
      name: 'filter',
      label: '3. Filtro de Dados',
      type: 'select',
      colSpan: 6, // Divide a linha
      validation: { required: 'Selecione o filtro' },
      options: [
        { label: 'Todos os Registros', value: 'all' },
        { label: 'Apenas Ativos', value: 'active' },
        { label: 'Arquivados / Inativos', value: 'archived' },
      ]
    }
  ];

  return (
    <Card className="admin-terminal-card">
      <Card.Body title="Terminal de Comando"> 
         {/* [cite: 39] Usando sub-componente Body */}
        <p style={{ marginBottom: '1rem', color: '#6c757d' }}>
            Configure os parâmetros abaixo para inicializar o ambiente administrativo desejado.
        </p>
        
        <GenericForm<TerminalFormData>
          fields={terminalFields}
          onSubmit={handleTerminalCommand}
          submitText="Executar Comando 🚀"
        />
      </Card.Body>
    </Card>
  );
};