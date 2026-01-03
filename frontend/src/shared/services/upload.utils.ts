import { ApiService } from "../services/api.service";

// --- CONFIGURAÇÕES ---
const CONFIG = {
  // Arquivos acima de 50MB são tratados como "Grandes" (vão um por um ou via chunks)
  LARGE_FILE_THRESHOLD: 50 * 1024 * 1024,
  // Arquivos acima de 200MB ativam o modo Chunk (Fatiamento) - *Requer suporte no Backend*
  HUGE_FILE_CHUNK_THRESHOLD: 200 * 1024 * 1024,
  CHUNK_SIZE: 5 * 1024 * 1024, // 5MB por pedaço
  BATCH_SIZE: 3, // Uploads simultâneos para arquivos pequenos
};

/**
 * Interface para retorno padronizado
 */
interface UploadResult {
  fileName: string;
  status: "success" | "error";
  data?: unknown;
  error?: unknown;
}

/**
 * Helper interno para enviar um arquivo fatiado (Chunking)
 * ATENÇÃO: Seu backend precisa saber montar os pedaços.
 */
const uploadInChunks = async (
  file: File,
  endpoint: string,
  extraData: Record<string, unknown>,
  fileKey: string
): Promise<unknown> => {
  const totalChunks = Math.ceil(file.size / CONFIG.CHUNK_SIZE);
  console.log(
    `[SmartUpload] Iniciando Chunking para ${file.name}: ${totalChunks} pedaços.`
  );

  let lastResponse = null;

  for (let i = 0; i < totalChunks; i++) {
    const start = i * CONFIG.CHUNK_SIZE;
    const end = Math.min(start + CONFIG.CHUNK_SIZE, file.size);
    const chunk = file.slice(start, end);

    // Cria um arquivo blob com o pedaço
    const chunkFile = new File([chunk], file.name, { type: file.type });

    // Adiciona metadados para o C# saber montar
    const chunkDto = {
      ...extraData,
      isChunk: true,
      chunkIndex: i,
      totalChunks: totalChunks,
      fileName: file.name,
    };

    // Envia o pedaço (usa o método base do ApiService para evitar loop infinito)
    // Nota: Aqui chamamos uma versão 'raw' se existir, ou o postWithFile assumindo que ele não vai chamar o smartUpload de volta recursivamente.
    // Para segurança, vamos assumir que o ApiService.postWithFileBase (método simples) será usado aqui.
    // Como não temos acesso ao método privado, usamos o postWithFile passando uma flag para ignorar a lógica smart.
    lastResponse = await ApiService.postWithFile(
      endpoint,
      chunkDto,
      chunkFile,
      fileKey,
      undefined,
      true
    );
  }

  return lastResponse;
};

/**
 * Gerenciador principal de Upload
 */
export const SmartUploadHandler = async (
  endpoint: string,
  data: Record<string, unknown>,
  files: File[],
  fileKey: string
): Promise<UploadResult[]> => {
  const results: UploadResult[] = [];

  // Separa arquivos em "Pequenos" e "Grandes"
  const smallFiles = files.filter((f) => f.size <= CONFIG.LARGE_FILE_THRESHOLD);
  const largeFiles = files.filter((f) => f.size > CONFIG.LARGE_FILE_THRESHOLD);

  // 1. Processa Arquivos PEQUENOS em Lotes (Paralelo)
  if (smallFiles.length > 0) {
    const queue = [...smallFiles];
    while (queue.length > 0) {
      const batch = queue.splice(0, CONFIG.BATCH_SIZE);

      const promises = batch.map((file) =>
        ApiService.postWithFile(endpoint, data, file, fileKey, undefined, true) // flag 'ignoreSmart'
          .then((res) => ({
            fileName: file.name,
            status: "success" as const,
            data: res,
          }))
          .catch((err) => ({
            fileName: file.name,
            status: "error" as const,
            error: err,
          }))
      );

      const batchResults = await Promise.all(promises);
      results.push(...batchResults);
    }
  }

  // 2. Processa Arquivos GRANDES um por um (Serial)
  for (const file of largeFiles) {
    try {
      let response;

      // Verifica se é GIGANTE para usar Chunking
      if (file.size > CONFIG.HUGE_FILE_CHUNK_THRESHOLD) {
        response = await uploadInChunks(file, endpoint, data, fileKey);
      } else {
        // Se for apenas Grande (ex: 60MB), envia inteiro mas sozinho na fila
        response = await ApiService.postWithFile(
          endpoint,
          data,
          file,
          fileKey,
          undefined,
          true
        );
      }

      results.push({ fileName: file.name, status: "success", data: response });
    } catch (err) {
      console.error(`Erro ao enviar arquivo grande: ${file.name}`, err);
      results.push({ fileName: file.name, status: "error", error: err });
    }
  }

  return results;
};
