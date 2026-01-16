import { useState, useCallback, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { SupportService } from "../services/support.service";
import { AlertService } from "../../../shared/services/alert.service";
import { ApiError } from "../../../shared/services/api.service";
import type {
  SupportTicket,
  CreateSupportTicketPayload,
  SupportTicketStatus,
} from "../types/support.types";

export const useSupport = () => {
  const [tickets, setTickets] = useState<SupportTicket[]>([]); // Lista geral
  const [currentTicket, setCurrentTicket] = useState<SupportTicket | null>(
    null
  );

  const navigate = useNavigate();

  // Estados de Paginação
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading] = useState(false);

  // Evita chamadas duplicadas (React.StrictMode pode disparar 2x)
  const loadingRef = useRef(false);

  // --- 1. CRIAR TICKET ---
  const createTicket = useCallback(
    async (payload: CreateSupportTicketPayload) => {
      setLoading(true);
      try {
        const response = await SupportService.createTicket(payload);
        if (response.success) {
          await AlertService.success(
            "Recebido!",
            "Seu ticket foi criado. Em breve entraremos em contato."
          );
          navigate("/perfil");
        }
      } catch (error) {
        const msg =
          error instanceof ApiError ? error.message : "Erro ao enviar ticket.";
        await AlertService.error("Erro", msg);
      } finally {
        setLoading(false);
      }
    },
    [navigate]
  );

  const fetchTicketsPaginated = useCallback(
    async (pageToLoad = 1, reset = false) => {
      if (loadingRef.current) return;

      loadingRef.current = true;
      setLoading(true);

      try {
        // Agora o TypeScript sabe que este método aceita (number, number)
        const response = await SupportService.getAllTickets(pageToLoad, 10);

        // E sabe que response.data tem .items, .totalPages, etc.
        if (response.success && response.data) {
          const { items, totalPages, currentPage } = response.data;

          setTickets((prev) => (reset ? items : [...prev, ...items]));
          setPage(currentPage);
          setHasMore(currentPage < totalPages);
        }
      } catch (error) {
        const msg =
          error instanceof ApiError ? error.message : "Erro ao carregar lista.";
        console.error(msg);
        AlertService.notify("Erro", msg, "error");
      } finally {
        setLoading(false);
        loadingRef.current = false;
      }
    },
    []
  );

  // --- BUSCAR POR ID ---
  const fetchTicketById = useCallback(async (id: string) => {
    setLoading(true);
    setCurrentTicket(null);
    try {
      const response = await SupportService.getTicketById(id);
      if (response.success && response.data) {
        setCurrentTicket(response.data);
      } else {
        AlertService.notify("Aviso", "Ticket não encontrado.", "warning");
      }
    } catch (error) {
      const msg =
          error instanceof ApiError ? error.message : "Erro ao carregar lista.";
      console.error(msg);
      AlertService.error("Erro", msg);
    } finally {
      setLoading(false);
    }
  }, []);

  // --- 4. ATUALIZAR STATUS ---
  const updateStatus = useCallback(
    async (ticketId: string, newStatus: SupportTicketStatus) => {
      const { isConfirmed } = await AlertService.confirm(
        "Alterar Status",
        `Deseja mudar este ticket para "${newStatus}"?`
      );

      if (!isConfirmed) return;

      setLoading(true);
      try {
        const response = await SupportService.updateTicketStatus(
          ticketId,
          newStatus
        );

        if (response.success) {
          AlertService.notify("Sucesso", "Status atualizado!", "success");

          // Atualiza a lista local
          setTickets((prev) =>
            prev.map((t) =>
              t.id === ticketId ? { ...t, status: newStatus } : t
            )
          );

          // Se estivermos visualizando este ticket agora, atualiza ele também
          setCurrentTicket((prev) =>
            prev && prev.id === ticketId ? { ...prev, status: newStatus } : prev
          );
        }
      } catch (error) {
        const msg =
          error instanceof ApiError
            ? error.message
            : "Erro ao atualizar status.";
        AlertService.error("Erro", msg);
      } finally {
        setLoading(false);
      }
    },
    []
  );

  return {
    tickets,
    currentTicket, // Exporta o novo estado
    loading,
    createTicket,
    hasMore,
    page,
    fetchTicketsPaginated,
    fetchTicketById,
    updateStatus,
  };
};
