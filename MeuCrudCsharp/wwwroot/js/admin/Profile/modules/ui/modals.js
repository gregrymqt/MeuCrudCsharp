// /js/admin/modules/ui/modals.js

/**
 * Abre um modal.
 * @param {HTMLElement} modalElement - O elemento do modal a ser aberto.
 */
export function openModal(modalElement) {
    if (modalElement) {
        modalElement.style.display = 'block';
    }
}

/**
 * Fecha um modal e opcionalmente reseta seu formulário interno.
 * @param {HTMLElement} modalElement - O elemento do modal a ser fechado.
 */
export function closeModal(modalElement) {
    if (modalElement) {
        modalElement.style.display = 'none';
        const form = modalElement.querySelector('form');
        if (form) {
            form.reset();
        }
    }
}