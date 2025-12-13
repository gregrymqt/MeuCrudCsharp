/**
 * Chaves conhecidas do sistema para evitar "magic strings"
 */
export const STORAGE_KEYS = {
  USER_SESSION: '@GregCompany:User',
  TOKEN: '@GregCompany:Token',
  THEME: '@GregCompany:Theme',
  CSRF_TOKEN: '@GregCompany:Csrf'
};

/**
 * Interface genérica para o serviço de cache
 */
interface IStorageService {
  getItem<T>(key: string): T | null;
  setItem<T>(key: string, value: T): void;
  removeItem(key: string): void;
  clear(): void;
}

export const StorageService: IStorageService = {
  /**
   * Busca um item do cache e faz o Parse do JSON automaticamente
   */
  getItem: <T>(key: string): T | null => {
    try {
      const item = localStorage.getItem(key);
      return item ? (JSON.parse(item) as T) : null;
    } catch (error) {
      console.error(`Erro ao ler do cache (${key}):`, error);
      return null;
    }
  },

  /**
   * Salva um item no cache transformando em JSON
   */
  setItem: <T>(key: string, value: T): void => {
    try {
      const jsonValue = JSON.stringify(value);
      localStorage.setItem(key, jsonValue);
    } catch (error) {
      console.error(`Erro ao salvar no cache (${key}):`, error);
    }
  },

  removeItem: (key: string): void => {
    localStorage.removeItem(key);
  },

  clear: (): void => {
    localStorage.clear();
  }
};