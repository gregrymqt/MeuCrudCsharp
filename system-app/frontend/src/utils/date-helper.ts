/**
 * Utilitário para manipulação de datas vindo do C# (ISO Strings)
 */

export const DateHelper = {
  /**
   * Converte string ISO para objeto Date nativo do JS
   * @param dateString Ex: "2025-12-11T14:30:00"
   */
  toDate: (dateString?: string): Date | null => {
    if (!dateString) return null;
    return new Date(dateString);
  },

  /**
   * Formata para padrão brasileiro: dd/MM/yyyy
   */
  formatDate: (dateString?: string): string => {
    const date = DateHelper.toDate(dateString);
    if (!date) return '-';
    
    return new Intl.DateTimeFormat('pt-BR').format(date);
  },

  /**
   * Formata para padrão brasileiro com hora: dd/MM/yyyy HH:mm
   */
  formatDateTime: (dateString?: string): string => {
    const date = DateHelper.toDate(dateString);
    if (!date) return '-';

    return new Intl.DateTimeFormat('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    }).format(date);
  }
};