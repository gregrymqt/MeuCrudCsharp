import Swal, { type SweetAlertIcon, type SweetAlertOptions } from 'sweetalert2';

// Cores do seu Sistema (SCSS Variables)
const COLORS = {
  primary: '#007bff',
  success: '#28a745',
  danger: '#dc3545',
  warning: '#ffc107',
  info: '#17a2b8',
  text: '#212529',
  bg: '#ffffff'
};

/**
 * Configuração padrão para Mobile-First
 * Botões grandes e fáceis de tocar
 */
const baseConfig: SweetAlertOptions = {
  confirmButtonColor: COLORS.primary,
  cancelButtonColor: COLORS.danger,
  color: COLORS.text,
  background: COLORS.bg,
  buttonsStyling: true,
  customClass: {
    confirmButton: 'swal-confirm-btn', // Podemos estilizar no global.scss se quiser
    cancelButton: 'swal-cancel-btn',
    popup: 'swal-mobile-popup' // Classe para ajustes finos no mobile
  }
};

// Mixin para Toast (Notificação de WebSocket)
const Toast = Swal.mixin({
  toast: true,
  position: 'top-end',
  showConfirmButton: false,
  timer: 4000,
  timerProgressBar: true,
  didOpen: (toast) => {
    toast.addEventListener('mouseenter', Swal.stopTimer);
    toast.addEventListener('mouseleave', Swal.resumeTimer);
  }
});

export const AlertService = {
  
  /**
   * 1. FASE CONCLUÍDA (Success)
   * Usado quando uma operação (POST/PUT) dá certo.
   */
  success: async <T>(title: string, message?: string, data?: T): Promise<T | undefined> => {
    await Swal.fire({
      ...baseConfig,
      icon: 'success',
      title: title,
      text: message,
      confirmButtonColor: COLORS.success,
      confirmButtonText: 'OK, Entendi'
    });
    return data;
  },

  /**
   * 2. FASE RECUSADA (Error)
   * Usado em catchs de API ou validações bloqueantes.
   */
  error: async <T>(title: string, message?: string, data?: T): Promise<T | undefined> => {
    await Swal.fire({
      ...baseConfig,
      icon: 'error',
      title: title,
      text: message,
      confirmButtonColor: COLORS.danger,
      confirmButtonText: 'Fechar'
    });
    return data;
  },

  /**
   * 3. FASE NOTIFICAÇÃO (WebSocket / Toast)
   * Não bloqueia a tela. Aparece no canto e some sozinha.
   * Ideal para: "Seu pagamento foi aprovado" ou "Novo curso disponível".
   */
  notify: <T>(title: string, message?: string, icon: SweetAlertIcon = 'info', data?: T): T | undefined => {
    Toast.fire({
      icon: icon,
      title: title,
      text: message
    });
    // Retorna o dado imediatamente pois o Toast não espera clique
    return data;
  },

  /**
   * EXTRA: Confirmação Genérica
   * Muito útil para Deletar itens.
   */
  confirm: async <T>(
    title: string, 
    message: string, 
    confirmText = 'Sim, confirmar',
    data?: T
  ): Promise<{ isConfirmed: boolean; data?: T }> => {
    const result = await Swal.fire({
      ...baseConfig,
      title: title,
      text: message,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: confirmText,
      cancelButtonText: 'Cancelar',
      confirmButtonColor: COLORS.primary,
      reverseButtons: true, // UX Mobile: Cancelar na esquerda, Confirmar na direita (padrão app)
    });

    return { isConfirmed: result.isConfirmed, data };
  }
};