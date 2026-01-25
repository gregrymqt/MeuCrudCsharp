// src/features/admin/courses/services/course.service.ts

import { ApiService } from 'src/shared/services/api.service'; // 
import type { Course } from 'src/types/models';
import type { CourseFilters, PaginatedResponse, CreateCourseData, UpdateCourseData } from '../types/course-manager.types';


const BASE_ENDPOINT = '/admin/courses'; // 

export const CourseService = {

    // GET: Listar com paginação [cite: 47]
    getAll: async (filters: CourseFilters): Promise<PaginatedResponse<Course>> => {
        // Monta a query string: ?pageNumber=1&pageSize=10
        const params = new URLSearchParams({
            pageNumber: filters.pageNumber.toString(),
            pageSize: filters.pageSize.toString(),
        });

        return await ApiService.get<PaginatedResponse<Course>>(`${BASE_ENDPOINT}?${params.toString()}`);
    },

    // GET: Buscar por ID [cite: 53]
    getById: async (id: string): Promise<Course> => {
        return await ApiService.get<Course>(`${BASE_ENDPOINT}/${id}`);
    },

    // GET: Pesquisar por nome (Search endpoint) [cite: 56]
    search: async (name: string): Promise<Course[]> => {
        return await ApiService.get<Course[]>(`${BASE_ENDPOINT}/search?name=${encodeURIComponent(name)}`);
    },

    // POST: Criar curso [cite: 60]
    create: async (data: CreateCourseData): Promise<Course> => {
        return await ApiService.post<Course>(BASE_ENDPOINT, data);
    },

    // PUT: Atualizar curso [cite: 63]
    update: async (id: string, data: UpdateCourseData): Promise<Course> => {
        return await ApiService.put<Course>(`${BASE_ENDPOINT}/${id}`, data);
    },

    // DELETE: Remover curso [cite: 66]
    delete: async (id: string): Promise<void> => {
        await ApiService.delete(`${BASE_ENDPOINT}/${id}`);
    }
};