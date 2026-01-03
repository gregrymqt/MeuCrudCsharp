import { StorageService, STORAGE_KEYS } from "./storage.service";
import { SmartUploadHandler } from "./upload.utils"; // <-- Importe o arquivo criado acima

// --- INTERFACES AUXILIARES ---
interface ApiErrorResponse {
  message?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

// --- 1. CLASSE DE ERRO PERSONALIZADA ---
export class ApiError extends Error {
  public status: number;
  public data: unknown;

  constructor(status: number, message: string, data?: unknown) {
    super(message);
    this.status = status;
    this.data = data;
    this.name = "ApiError";
  }
}

const toFormData = <T extends Record<string, unknown>>(
  data: T,
  files?: File | File[] | FileList | null, // Aceita 1 ou vários
  fileKey: string = "files" // Nome do campo (No C# use List<IFormFile> files)
): FormData => {
  const formData = new FormData();

  // 1. Anexa os dados do objeto (DTO)
  Object.entries(data).forEach(([key, value]) => {
    if (value === undefined || value === null) return;

    if (value instanceof Date) {
      formData.append(key, value.toISOString());
    } else if (typeof value === "object" && !(value instanceof File)) {
      formData.append(key, JSON.stringify(value));
    } else {
      formData.append(key, String(value));
    }
  });

  // 2. Anexa os arquivos
  if (files) {
    if (files instanceof FileList) {
      // Se vier direto do <input type="file" multiple>
      Array.from(files).forEach((file) => formData.append(fileKey, file));
    } else if (Array.isArray(files)) {
      // Se vier de um array [File, File]
      files.forEach((file) => formData.append(fileKey, file));
    } else {
      // Se for apenas um arquivo único
      formData.append(fileKey, files);
    }
  }

  return formData;
};

// --- 3. CONFIGURAÇÃO BASE ---
const BASE_URL = "/api";

const getHeaders = (isFormData = false): HeadersInit => {
  const headers: Record<string, string> = {
    "ngrok-skip-browser-warning": "true",
  };

  // Se for FormData, NÃO definimos 'Content-Type'.
  // O navegador define automaticamente como multipart/form-data com o boundary correto.
  if (!isFormData) {
    headers["Content-Type"] = "application/json";
    headers["Accept"] = "application/json";
  }

  const token = StorageService.getItem<string>(STORAGE_KEYS.TOKEN);
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const csrfToken = StorageService.getItem<string>(STORAGE_KEYS.CSRF_TOKEN);
  if (csrfToken) {
    headers["X-CSRF-TOKEN"] = csrfToken;
  }

  return headers;
};

// --- 4. TRATAMENTO DE RESPOSTA E ERROS ---
const handleResponse = async <T>(response: Response): Promise<T> => {
  let data: unknown = null;
  const contentType = response.headers.get("content-type");

  if (contentType && contentType.includes("application/json")) {
    data = await response.json();
  } else {
    data = await response.text();
  }

  if (response.ok) {
    return data as T;
  }

  const errorData = data as ApiErrorResponse;
  let errorMessage = "Ocorreu um erro inesperado.";

  switch (response.status) {
    case 400:
      if (errorData && errorData.errors) {
        errorMessage = Object.values(errorData.errors).flat().join(", ");
      } else if (typeof errorData?.message === "string") {
        errorMessage = errorData.message;
      } else {
        errorMessage = "Dados inválidos. Verifique os campos.";
      }
      break;

    case 401:
      errorMessage = "Sessão expirada. Faça login novamente.";
      window.dispatchEvent(new Event("auth:logout"));
      break;

    case 403:
      errorMessage = "Você não tem permissão para realizar esta ação.";
      break;

    case 404:
      errorMessage = "Recurso não encontrado.";
      break;

    case 422:
      errorMessage = "Não foi possível processar as instruções presentes.";
      break;

    case 500:
      errorMessage = "Erro interno no servidor. Tente novamente mais tarde.";
      break;

    default:
      if (typeof errorData?.message === "string") {
        errorMessage = errorData.message;
      } else {
        errorMessage = response.statusText || errorMessage;
      }
  }

  throw new ApiError(response.status, errorMessage, data);
};

// --- 5. O WRAPPER API (MÉTODOS) ---
export const ApiService = {
  // --- MÉTODOS JSON PADRÃO ---

  get: async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
    const headers = {
      ...getHeaders(),
      ...((options?.headers as Record<string, string>) || {}),
    };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: "GET",
      headers: headers as HeadersInit,
    });
    return await handleResponse<T>(response);
  },

  post: async <TResponse, TBody = unknown>(
    endpoint: string,
    body: TBody,
    options?: RequestInit
  ): Promise<TResponse> => {
    const headers = {
      ...getHeaders(),
      ...((options?.headers as Record<string, string>) || {}),
    };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: "POST",
      headers: headers as HeadersInit,
      body: JSON.stringify(body),
    });
    return await handleResponse<TResponse>(response);
  },

  put: async <TResponse, TBody = unknown>(
    endpoint: string,
    body: TBody,
    options?: RequestInit
  ): Promise<TResponse> => {
    const headers = {
      ...getHeaders(),
      ...((options?.headers as Record<string, string>) || {}),
    };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: "PUT",
      headers: headers as HeadersInit,
      body: JSON.stringify(body),
    });
    return await handleResponse<TResponse>(response);
  },

  delete: async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
    const headers = {
      ...getHeaders(),
      ...((options?.headers as Record<string, string>) || {}),
    };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: "DELETE",
      headers: headers as HeadersInit,
    });
    return await handleResponse<T>(response);
  },

  /**
   * Envia DTO + Arquivos.
   * - Se for 1 arquivo pequeno: Envia direto.
   * - Se forem muitos ou grandes: Usa o SmartUploadHandler.
   */
  postWithFile: async <TResponse, TBody extends Record<string, unknown>>(
    endpoint: string,
    data: TBody,
    files?: File | File[] | FileList | null,
    fileKey: string = "files",
    options?: RequestInit,
    bypassSmartLogic: boolean = false // <-- NOVO PARÂMETRO DE CONTROLE
  ): Promise<TResponse> => {
    // Normalização dos arquivos para Array
    let fileArray: File[] = [];
    if (files) {
      if (files instanceof FileList) fileArray = Array.from(files);
      else if (Array.isArray(files)) fileArray = files;
      else fileArray = [files];
    }

    // LÓGICA DE DECISÃO:
    // Se não devemos ignorar a lógica smart E (temos mais de 1 arquivo OU temos 1 arquivo grande > 50MB)
    const isComplexUpload =
      fileArray.length > 1 ||
      (fileArray.length === 1 && fileArray[0].size > 50 * 1024 * 1024);

    if (!bypassSmartLogic && isComplexUpload) {
      console.log("Detectado upload complexo. Usando SmartHandler...");
      // Chama o gerenciador e retorna o resultado (pode precisar adaptar o retorno dependendo do que sua tela espera)
      // Aqui retornamos o array de resultados, ou o primeiro sucesso se for unitário.
      const results = await SmartUploadHandler(
        endpoint,
        data,
        fileArray,
        fileKey
      );

      // Se deu erro em tudo, lança erro. Se deu sucesso parcial, retorna o que deu.
      const errors = results.filter((r) => r.status === "error");
      if (errors.length === results.length) throw errors[0].error;

      return results as unknown as TResponse;
    }

    // --- CAMINHO RÁPIDO (FAST PATH) ---
    // Cria o FormData e envia direto (fetch normal)
    const formData = toFormData(data, files, fileKey);
    const headers = {
      ...getHeaders(true),
      ...((options?.headers as Record<string, string>) || {}),
    };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: "POST",
      headers: headers as HeadersInit,
      body: formData,
    });
    return await handleResponse<TResponse>(response);
  },

  // Faça o mesmo para o putWithFile se desejar
  putWithFile: async <TResponse, TBody extends Record<string, unknown>>(
    endpoint: string,
    data: TBody,
    files?: File | File[] | FileList | null,
    fileKey: string = "files",
    options?: RequestInit,
    bypassSmartLogic: boolean = false
  ): Promise<TResponse> => {
    // ... mesma lógica de normalização de array ...
    let fileArray: File[] = [];
    if (files) {
      if (files instanceof FileList) fileArray = Array.from(files);
      else if (Array.isArray(files)) fileArray = files;
      else fileArray = [files];
    }

    const isComplexUpload =
      fileArray.length > 1 ||
      (fileArray.length === 1 && fileArray[0].size > 50 * 1024 * 1024);

    if (!bypassSmartLogic && isComplexUpload) {
      const results = await SmartUploadHandler(
        endpoint,
        data,
        fileArray,
        fileKey
      );
      return results as unknown as TResponse;
    }

    const formData = toFormData(data, files, fileKey);
    const headers = {
      ...getHeaders(true),
      ...((options?.headers as Record<string, string>) || {}),
    };

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      method: "PUT",
      headers: headers as HeadersInit,
      body: formData,
    });
    return await handleResponse<TResponse>(response);
  },
};
