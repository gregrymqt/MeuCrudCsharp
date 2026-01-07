// src/features/admin/courses/hooks/useCourses.ts

import { useState, useCallback } from 'react';
import { CourseService } from '../services/course.service';
import { AlertService } from '../../../../shared/services/alert.service'; // [cite: 31]
import { ApiError } from '../../../../shared/services/api.service'; // [cite: 2]
import type { Course } from '../../../../types/models';
import type { PaginatedResponse, CreateCourseData, UpdateCourseData } from '../types/course-manager.types';

export const useCourses = () => {
    const [loading, setLoading] = useState(false);
    const [coursesData, setCoursesData] = useState<PaginatedResponse<Course> | null>(null);

    // Função para carregar cursos (paginado)
    const fetchCourses = useCallback(async (page = 1, pageSize = 10) => {
        setLoading(true);
        try {
            const response = await CourseService.getAll({ pageNumber: page, pageSize }); // [cite: 47]
            setCoursesData(response);
        } catch (error) {
            if (error instanceof ApiError && error.status === 404) {
                AlertService.error('Erro', error.message);
            }
        } finally {
            setLoading(false);
        }
    }, []);

    // Função para criar curso
    const createCourse = async (data: CreateCourseData) => {
        setLoading(true);
        try {
            await CourseService.create(data); // [cite: 60]
            await AlertService.success('Sucesso!', 'Curso criado com sucesso.'); // [cite: 32]
            return true;
        } catch (error) {
            const msg = error instanceof ApiError ? error.message : 'Erro ao criar curso.';
            AlertService.error('Erro', msg);
            return false;
        } finally {
            setLoading(false);
        }
    };

    // Função para atualizar curso
    const updateCourse = async (id: string, data: UpdateCourseData) => {
        setLoading(true);
        try {
            await CourseService.update(id, data); // [cite: 63]
            await AlertService.success('Atualizado!', 'Dados do curso salvos.');
            return true;
        } catch (error) {
            const msg = error instanceof ApiError ? error.message : 'Erro ao atualizar curso.';
            AlertService.error('Erro', msg);
            return false;
        } finally {
            setLoading(false);
        }
    };

    // Função para deletar curso com validação de conflito (Vídeos)
    const deleteCourse = async (id: string) => {
        // 1. Confirmação Visual [cite: 43]
        const { isConfirmed } = await AlertService.confirm(
            'Tem certeza?',
            'Esta ação removerá o curso permanentemente.'
        );

        if (!isConfirmed) return false;

        setLoading(true);
        try {
            await CourseService.delete(id); // [cite: 66]
            await AlertService.success('Deletado!', 'O curso foi removido.');

            // Atualiza a lista local removendo o item deletado para evitar refresh desnecessário
            if (coursesData) {
                setCoursesData({
                    ...coursesData,
                    items: coursesData.items.filter(c => c.publicId !== id)
                });
            }
            return true;
        } catch (error) {
            // TRATAMENTO ESPECIAL DO BACKEND [cite: 68]
            // Se o backend retornar 409 (Conflict), é porque tem vídeos associados.
            if (error instanceof ApiError && error.status === 409) {
                AlertService.error(
                    'Ação Bloqueada',
                    'Não é possível deletar este curso pois ele possui vídeos associados.'
                );
            } else {
                AlertService.error('Erro', 'Falha ao deletar o curso.');
            }
            return false;
        } finally {
            setLoading(false);
        }
    };

    return {
        courses: coursesData?.items || [],
        pagination: {
            pageNumber: coursesData?.pageNumber || 1,
            totalPages: coursesData?.totalPages || 1,
            totalCount: coursesData?.totalCount || 0
        },
        loading,
        fetchCourses,
        createCourse,
        updateCourse,
        deleteCourse
    };
};