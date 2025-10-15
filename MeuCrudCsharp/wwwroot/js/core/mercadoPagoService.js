/**
 * Este módulo centraliza toda a lógica de interação com o SDK do Mercado Pago.
 * Ele garante que o SDK seja inicializado apenas uma vez (padrão Singleton)
 * e fornece uma maneira segura de acessar a instância do SDK.
 */

import apiService from './apiService.js';

// --- Variáveis de Estado do Módulo ---

// Guardaremos a instância do SDK aqui para não precisar recriá-la.
let mpInstance = null;

// --- Funções "Privadas" do Módulo (Não exportadas) ---

/**
 * Busca a chave pública da nossa API.
 * Fica aqui dentro pois é um detalhe de implementação da inicialização.
 * @returns {Promise<string>} A chave pública.
 */
async function fetchPublicKey() {
    try {
        const responseData = await apiService.fetch('/api/configuracao/public-key');
        if (!responseData || !responseData.publicKey) {
            throw new Error("A resposta da API para a chave pública está mal formatada.");
        }
        return responseData.publicKey;
    } catch (error) {
        console.error("Erro ao buscar a Public Key:", error.message);
        // Propaga o erro com uma mensagem mais específica.
        throw new Error(`Falha ao obter a Public Key: ${error.message}`);
    }
}


// --- Funções Públicas (Exportadas para o resto da aplicação) ---

/**
 * Inicializa o SDK do Mercado Pago, se ainda não tiver sido inicializado.
 * @returns {Promise<MercadoPago>} A instância do SDK.
 */
export async function initializeMercadoPago() {
    // Se a instância já existe, apenas a retorna para evitar reprocessamento.
    if (mpInstance) {
        return mpInstance;
    }
    
    const publicKey = await fetchPublicKey();

    try {

        if (typeof publicKey !== 'string' || publicKey.trim() === '') {
            throw new Error("A Public Key retornada é inválida ou vazia.");
        }

        console.log("Public Key recebida. Inicializando o Mercado Pago...");
        mpInstance = new window.MercadoPago(publicKey);

        return mpInstance;

    } catch (error) {
        console.error("Falha CRÍTICA ao inicializar o Mercado Pago:", error);
        // Joga o erro para cima para que a lógica da página possa tratar (ex: mostrar uma UI de erro).
        throw error;
    }
}

/**
 * Retorna a instância já criada do SDK do Mercado Pago.
 * É a forma segura de acessar o SDK depois de inicializado.
 * @returns {MercadoPago} A instância do SDK.
 * @throws {Error} Se tentar obter a instância antes de inicializar.
 */
export function getMercadoPagoInstance() {
    if (!mpInstance) {
        throw new Error("O SDK do Mercado Pago não foi inicializado. Chame 'initializeMercadoPago' primeiro.");
    }
    return mpInstance;
}