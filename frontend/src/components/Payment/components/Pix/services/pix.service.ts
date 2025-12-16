import { ApiService } from "../../../../../shared/services/api.service";
import type {
  CreatePixDTO,
  PixResponse,
  IdentificationType,
} from "../types/pix.types";

export const PixService = {
  createPix: async (data: CreatePixDTO): Promise<PixResponse> => {
    // 1. Geramos uma chave única para garantir que não haja cobrança duplicada
    const idempotencyKey = self.crypto.randomUUID();
    const headers = {
      "X-Idempotency-Key": idempotencyKey,
    };
    // 2. Enviamos o Header que o Backend exige
    // Nota: Estou assumindo que seu ApiService aceita um terceiro parâmetro de config (como o axios)
    // Se o seu ApiService for customizado, precisaremos ajustar isso lá.
    return await ApiService.post<PixResponse>("/pix/createpix", data, {
      headers,
    });
  },

  getDocTypes: async (): Promise<IdentificationType[]> => {
    return [
      { id: "CPF", name: "CPF" },
      { id: "CNPJ", name: "CNPJ" },
    ];
  },
};
