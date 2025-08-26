/**
 * 1. Busca um token em um cookie específico.
 * @param {string} name - O nome do cookie (ex: 'jwt').
 * @returns {string|null} - O valor do token ou null.
 */
 function getCookie(name) {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) return parts.pop().split(';').shift();
    return null;
}

/**
 * 2. (NOVO) Busca um token de um parâmetro na URL.
 * Exemplo de URL: https://meusite.com/pagina?token=ABC123XYZ
 * @param {string} paramName - O nome do parâmetro na URL (ex: 'token').
 * @returns {string|null} - O valor do token ou null.
 */
function getTokenFromUrl(paramName) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(paramName);
}

/**
 * 3. (NOVO E MELHORADO) Orquestrador para obter o token de autenticação.
 * Tenta primeiro da URL, depois do cookie.
 * @returns {string|null} - O token encontrado ou null.
 */
export function getAuthToken() {
    // Adicione aqui os nomes que você usa. Ex: 'auth_token', 'jwt', etc.
    const tokenFromUrl = getTokenFromUrl('token'); 
    if (tokenFromUrl) {
        return tokenFromUrl;
    }
    
    const tokenFromCookie = getCookie('jwt');
    if (tokenFromCookie) {
        return tokenFromCookie;
    }

    return null; // Retorna null se não encontrar em lugar nenhum
}