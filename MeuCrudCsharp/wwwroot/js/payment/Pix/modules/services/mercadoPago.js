// js/modules/mercadoPago.js
import { createSelectOptions } from '../ui/ui.js';

let mpInstance;

/**
 * Inicializa o SDK do Mercado Pago com a Public Key.
 * @param {string} publicKey - A chave pública.
 * @returns {Promise<MercadoPago>} A instância do SDK.
 */
export async function initializeMercadoPago(publicKey) {
    // A função loadMercadoPago não é mais necessária na v2 do SDK,
    // basta instanciar diretamente após carregar o script.
    mpInstance = new window.MercadoPago(publicKey);
    return mpInstance;
}

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