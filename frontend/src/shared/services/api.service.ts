import { StorageService, STORAGE_KEYS } from './storage.service';

// --- INTERFACES AUXILIARES ---
// Define o formato esperado de um erro vindo da API para evitar usar 'any'
interface ApiErrorResponse {
  message?: string;
  errors?: Record<string, string[]>; // Para erros de validação do ASP.NET
  [key: string]: unknown;
}

// --- 1. CLASSE DE ERRO PERSONALIZADA ---
export class ApiError extends Error {
  public status: number;
  public data: unknown; // [Corrigido]: any -> unknown

  constructor(status: number, message: string, data?: unknown) { // [Corrigido]: any -> unknown
    super(message);
    this.status = status;
    this.data = data;
    this.name = 'ApiError';
  }
}

// --- 2. CONFIGURAÇÃO BASE ---
const BASE_URL = '/api';

const getHeaders = (isFormData = false): HeadersInit => {
  const headers: HeadersInit = {
    'ngrok-skip-browser-warning': 'true',
  };

  if (!isFormData) {
    headers['Content-Type'] = 'application/json';
    headers['Accept'] = 'application/json';
  }

  const token = StorageService.getItem<string>(STORAGE_KEYS.TOKEN);
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const csrfToken = StorageService.getItem<string>(STORAGE_KEYS.CSRF_TOKEN);
  if (csrfToken) {
    headers['X-CSRF-TOKEN'] = csrfToken;
  }

  return headers;
};

// --- 3. TRATAMENTO DE RESPOSTA E ERROS ---
const handleResponse = async <T>(response: Response): Promise<T> => {
  let data: unknown = null; // [Corrigido]: any -> unknown
  const contentType = response.headers.get('content-type');
  
  // Tenta fazer o parse de acordo com o tipo
  if (contentType && contentType.includes('application/json')) {
    data = await response.json();
  } else {
    data = await response.text();
  }

  if (response.ok) {
    return data as T;
  }

  // --- 4. TRATAMENTO DE ERROS ---
  // Fazemos um cast seguro para nossa interface de erro para ler as propriedades
  const errorData = data as ApiErrorResponse; 
  let errorMessage = 'Ocorreu um erro inesperado.';

  switch (response.status) {
    case 400: // Bad Request
      if (errorData && errorData.errors) {
        // Pega as mensagens de erro do ASP.NET (ValidationProblemDetails)
        errorMessage = Object.values(errorData.errors).flat().join(', ');
      } else if (typeof errorData?.message === 'string') {
        errorMessage = errorData.message;
      } else {
        errorMessage = 'Dados inválidos. Verifique os campos.';
      }
      break;

    case 401: // Unauthorized
      errorMessage = 'Sessão expirada. Faça login novamente.';
      window.dispatchEvent(new Event('auth:logout'));
      break;

    case 403: // Forbidden
      errorMessage = 'Você não tem permissão para realizar esta ação.';
      break;

    case 404: // Not Found
      errorMessage = 'Recurso não encontrado.';
      break;

    case 422: // Unprocessable Entity
      errorMessage = 'Não foi possível processar as instruções presentes.';
      break;
      
    case 500: // Internal Server Error
      errorMessage = 'Erro interno no servidor. Tente novamente mais tarde.';
      break;

    default:
      // Tenta pegar a mensagem de erro genérica ou o status text
      if (typeof errorData?.message === 'string') {
        errorMessage = errorData.message;
      } else {
        errorMessage = response.statusText || errorMessage;
      }
  }

  throw new ApiError(response.status, errorMessage, data);
};

// --- 5. O WRAPPER API (MÉTODOS CRUD) ---
export const ApiService = {
  
  // GET Genérico
  get: async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
    const headers = { ...(getHeaders() as Record<string, string>), ...(options?.headers || {}) };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: 'GET',
      headers: headers as HeadersInit,
    });
    return await handleResponse<T>(response);
  },

  // POST JSON Genérico
  // [Atualizado]: Aceita options para passar headers extras (ex: Idempotency-Key)
  post: async <T>(endpoint: string, body: unknown, options?: RequestInit): Promise<T> => {
    // Mescla os headers padrão com os headers passados no options
    const headers = { ...(getHeaders() as Record<string, string>), ...(options?.headers || {}) };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: 'POST',
      headers: headers as HeadersInit,
      body: JSON.stringify(body),
    });
    return await handleResponse<T>(response);
  },

  // PUT JSON Genérico
  put: async <T>(endpoint: string, body: unknown, options?: RequestInit): Promise<T> => {
    const headers = { ...(getHeaders() as Record<string, string>), ...(options?.headers || {}) };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: 'PUT',
      headers: headers as HeadersInit,
      body: JSON.stringify(body),
    });
    return await handleResponse<T>(response);
  },

  // DELETE Genérico
  delete: async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
    const headers = { ...(getHeaders() as Record<string, string>), ...(options?.headers || {}) };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: 'DELETE',
      headers: headers as HeadersInit,
    });
    return await handleResponse<T>(response);
  },

  // --- MÉTODOS ESPECIAIS PARA FORM-DATA ---
  
  postFormData: async <T>(endpoint: string, formData: FormData, options?: RequestInit): Promise<T> => {
    const headers = { ...(getHeaders(true) as Record<string, string>), ...(options?.headers || {}) };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: 'POST',
      headers: headers as HeadersInit,
      body: formData,
    });
    return await handleResponse<T>(response);
  },

  putFormData: async <T>(endpoint: string, formData: FormData, options?: RequestInit): Promise<T> => {
    const headers = { ...(getHeaders(true) as Record<string, string>), ...(options?.headers || {}) };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: 'PUT',
      headers: headers as HeadersInit,
      body: formData,
    });
    return await handleResponse<T>(response);
  }
};