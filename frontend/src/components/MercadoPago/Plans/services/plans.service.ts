import { ApiService } from "../../../../shared/services/api.service";
import type { PagedResult, PlanSummary, PlanEditDetail, CreatePlanRequest, UpdatePlanRequest } from "../types/plans.type";


const ENDPOINT = '/admin/plans';

export const PlanService = {

    // GET: Listar planos com paginação
    // C# Controller: GetPlans([FromQuery] int page = 1, [FromQuery] int pageSize = 10) [cite: 22]
    getAll: async (page: number = 1, pageSize: number = 10): Promise<PagedResult<PlanSummary>> => {
        // Monta a query string
        const query = `?page=${page}&pageSize=${pageSize}`;
        return await ApiService.get<PagedResult<PlanSummary>>(`${ENDPOINT}${query}`);
    },

    // GET: Obter plano específico para edição
    // C# Controller: GetPlanById(Guid id) -> retorna PlanEditDto [cite: 24]
    getById: async (id: string): Promise<PlanEditDetail> => {
        return await ApiService.get<PlanEditDetail>(`${ENDPOINT}/${id}`);
    },

    // POST: Criar novo plano
    // C# Controller: CreatePlan([FromBody] CreatePlanDto createDto) [cite: 19]
    create: async (data: CreatePlanRequest): Promise<unknown> => {
        return await ApiService.post(`${ENDPOINT}`, data);
    },

    // PUT: Atualizar plano existente
    // C# Controller: UpdatePlan(Guid id, [FromBody] UpdatePlanDto updateDto) [cite: 27]
    update: async (id: string, data: UpdatePlanRequest): Promise<unknown> => {
        return await ApiService.put(`${ENDPOINT}/${id}`, data);
    },

    // DELETE: Remover plano
    // C# Controller: DeletePlan(Guid id) [cite: 30]
    delete: async (id: string): Promise<void> => {
        return await ApiService.delete(`${ENDPOINT}/${id}`);
    }
};