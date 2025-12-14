import { type ReactNode } from 'react';
import './Table.scss';

// Definição de uma Coluna
export interface TableColumn<T> {
  header: string; // Título da coluna (ex: "Nome", "Preço")
  accessor?: keyof T; // A chave direta no objeto (ex: "name", "price")
  
  // Render customizado (opcional). Útil para formatar datas, moedas ou botões.
  render?: (item: T) => ReactNode; 
  
  // Largura opcional (ex: "100px" ou "20%")
  width?: string;
}

interface TableProps<T> {
  data: T[];
  columns: TableColumn<T>[];
  keyExtractor: (item: T) => string | number; // Identificador único da linha
  
  isLoading?: boolean; // Para mostrar loading state
  emptyMessage?: string; // Mensagem se não tiver dados
}

export const Table = <T,>({ 
  data, 
  columns, 
  keyExtractor,
  isLoading = false,
  emptyMessage = "Nenhum dado encontrado."
}: TableProps<T>) => {

  if (isLoading) {
    return <div className="p-4 text-center">Carregando dados...</div>;
  }

  if (!data || data.length === 0) {
    return <div className="p-4 text-center text-muted">{emptyMessage}</div>;
  }

  return (
    <div className="generic-table-container">
      <table className="generic-table">
        <thead>
          <tr>
            {columns.map((col, index) => (
              <th key={index} style={{ width: col.width }}>
                {col.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.map((item) => (
            <tr key={keyExtractor(item)}>
              {columns.map((col, index) => (
                <td key={index} data-label={col.header}>
                  {/* Lógica: Se tiver render customizado, usa. Se não, usa o accessor direto. */}
                  {col.render 
                    ? col.render(item) 
                    : (col.accessor ? String(item[col.accessor]) : '-')}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};