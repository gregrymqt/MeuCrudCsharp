// js/modules/mercadoPago.js
import { createSelectOptions } from '../ui/ui.js';

/**
 * Busca e popula os tipos de documento de identificação.
 */
export async function loadIdentificationTypes() {
    try {
        const identificationTypes = await mpInstance.getIdentificationTypes();
        const selectElement = document.getElementById('form-checkout__identificationType');
        createSelectOptions(selectElement, identificationTypes);
    } catch (e) {
        console.error('Error getting identificationTypes: ', e);
        // Opcional: mostrar erro na UI
    }
}