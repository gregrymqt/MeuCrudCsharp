// /js/core/cacheService.js

/**
 * Um serviço de cache que usa sessionStorage e tem tempo de expiração.
 * Os dados persistem durante a sessão do navegador (até a aba ser fechada),
 * mas expiram após um tempo definido para garantir que sejam atualizados.
 */

/**
 * Salva um valor no cache com uma duração de validade.
 * @param {string} key - A chave para o item do cache.
 * @param {any} value - O valor a ser armazenado.
 * @param {number} duration - Duração em milissegundos. Padrão: 5 minutos.
 */
function set(key, value, duration = 300000) { // Padrão de 5 minutos
    const expiry = Date.now() + duration;
    const item = {
        value: value,
        expiry: expiry,
    };
    sessionStorage.setItem(key, JSON.stringify(item));
}

/**
 * Recupera um valor do cache, se ele não tiver expirado.
 * @param {string} key - A chave do item a ser recuperado.
 * @returns {any|null} - O valor armazenado ou null se não existir ou tiver expirado.
 */
function get(key) {
    const itemStr = sessionStorage.getItem(key);

    if (!itemStr) {
        return null;
    }

    const item = JSON.parse(itemStr);
    const now = Date.now();

    // Verifica se o item expirou
    if (now > item.expiry) {
        sessionStorage.removeItem(key); // Limpa o item expirado
        return null;
    }

    return item.value;
}

/**
 * Remove um item específico do cache.
 * @param {string} key - A chave do item a ser removido.
 */
function remove(key) {
    sessionStorage.removeItem(key);
}

/**
 * Limpa todo o cache do sessionStorage.
 */
function clear() {
    // Cuidado: isso limpará todo o sessionStorage, não apenas o nosso cache.
    // Para um controle mais fino, seria necessário um prefixo nas chaves.
    sessionStorage.clear();
}

const cacheService = {
    set,
    get,
    remove,
    clear,
};

export default cacheService;