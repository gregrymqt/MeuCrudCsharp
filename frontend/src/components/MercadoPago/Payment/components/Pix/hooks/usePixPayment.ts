import { useState, useEffect } from "react";
import { PixService } from "../services/pix.service";
import { AlertService } from "../../../../../../shared/services/alert.service"; // Ajuste caminho
import { socketService } from "../../../../../../shared/services/socket.service"; // Ajuste caminho

import {
  type PixStep,
  type PixResponse,
  type IdentificationType,
  type PixPayer,
  type CreatePixDTO,
} from "../types/pix.types";
import { ApiError } from "../../../../../../shared/services/api.service";
import { AppHubs } from "../../../../../../shared/enums/hub.enums";
import { useSocketListener } from "../../../../../../shared/hooks/useSocket";
import type { PaymentSocketMessage } from "../../../types/payment.types";

interface UsePixPaymentProps {
  amount: number;
  description: string;
  onSuccess?: () => void;
}

export const usePixPayment = ({
  amount,
  description,
  onSuccess,
}: UsePixPaymentProps) => {
  const [step, setStep] = useState<PixStep>("FORM");
  const [loading, setLoading] = useState(false);
  const [pixData, setPixData] = useState<PixResponse | null>(null);
  const [docTypes, setDocTypes] = useState<IdentificationType[]>([]);
  const [error, setError] = useState<string | null>(null);

  // 1. Conectar ao WebSocket ao iniciar o fluxo
  useEffect(() => {
    // Conecta ao Hub de Pagamento
    socketService.connect(AppHubs.Payment);

    // Opcional: Desconectar ao sair da tela para economizar recursos
    return () => {
      // socketService.disconnect(AppHubs.Payment);
      // Comentei o disconnect pois às vezes o usuário navega e queremos manter a conexão,
      // mas no seu caso de uso de checkout, pode desconectar se quiser.
    };
  }, []);

  // 2. Ouvir atualizações do Backend (UpdatePaymentStatus)
  useSocketListener<PaymentSocketMessage>(
    AppHubs.Payment,
    "UpdatePaymentStatus",
    (data) => {
      console.log("⚡ WebSocket Update:", data);

      // Feedback visual não obstrutivo (Toasts)
      if (data.status === "processing") {
        AlertService.notify("Processando", data.message, "info");
      }

      // Sucesso!
      if (data.status === "approved") {
        AlertService.notify("Sucesso!", "Pagamento confirmado.", "success");
        setStep("APPROVED");
        if (onSuccess) onSuccess();
      }

      // Falha
      if (data.status === "failed" || data.status === "error") {
        AlertService.notify("Atenção", data.message, "error");
        // Não resetamos o step para QR_CODE imediatamente para dar tempo de ler,
        // mas o usuário pode tentar novamente gerando novo pix se necessário.
      }
    }
  );

  // Carregar documentos
  useEffect(() => {
    const loadDocs = async () => {
      try {
        const types = await PixService.getDocTypes();
        setDocTypes(types);
      } catch (e) {
        console.error("Erro ao carregar documentos", e);
      }
    };
    loadDocs();
  }, []);

  // Criação do PIX (API REST)
  const handleCreatePix = async (formData: PixPayer) => {
    setLoading(true);
    setError(null);

    try {
      const payload: CreatePixDTO = {
        transactionAmount: amount,
        description: description,
        payer: {
          firstName: formData.firstName,
          lastName: formData.lastName,
          email: formData.email,
          identification: {
            type: formData.identificationType,
            number: formData.identificationNumber,
          },
        },
      };

      // Aqui entra o Header de Idempotência que ajustamos anteriormente
      const response = await PixService.createPix(payload);

      setPixData(response);
      setStep("QR_CODE");

      // Feedback imediato
      AlertService.notify("PIX Gerado", "Aguardando pagamento...", "info");
    } catch (err) {
      // Se der erro na API (antes do socket)
      if (err instanceof ApiError) {
        setError(err.message);
        AlertService.error("Erro ao criar PIX", err.message);
        return;
      }
    } finally {
      setLoading(false);
    }
  };

  const copyPixCode = async () => {
    if (pixData?.qrCode) {
      await navigator.clipboard.writeText(pixData.qrCode);
      AlertService.notify(
        "Copiado!",
        "Código PIX copiado para a área de transferência.",
        "success"
      );
      return true;
    }
    return false;
  };

  return {
    step,
    loading,
    pixData,
    docTypes,
    error,
    handleCreatePix,
    copyPixCode,
    setStep,
  };
};
