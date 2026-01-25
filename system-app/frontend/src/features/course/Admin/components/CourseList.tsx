import React from 'react';
import { type Course } from 'src/types/models';
import styles from '../styles/CourseList.module.scss';
import { type CourseListProps } from '../types/course-manager.types';
import { ActionMenu } from 'src/components/ActionMenu/ActionMenu';
import { type TableColumn, Table } from 'src/components/Table/Table';

export const CourseList: React.FC<CourseListProps> = ({ 
  courses, 
  isLoading, 
  onEdit, 
  onDelete, 
  onNewClick 
}) => {

  const columns: TableColumn<Course>[] = [
    { 
      header: 'Nome do Curso', 
      accessor: 'name',
      width: '40%' 
    },
    { 
      header: 'Descrição', 
      accessor: 'description',
      width: '40%',
      render: (course) => (
        <span title={course.description}>
          {course.description.length > 50 
            ? `${course.description.substring(0, 50)}...` 
            : course.description}
        </span>
      )
    },
    {
      header: 'Ações',
      width: '20%',
      render: (course) => (
        <ActionMenu 
          onEdit={() => onEdit(course)}
          // [CORREÇÃO]: Usando publicId para deletar, pois é o GUID do banco
          onDelete={() => onDelete(course.publicId)} 
        />
      )
    }
  ];

  return (
    <div className={styles.listContainer}>
      <div className={styles.header}>
        <h2>Gerenciar Cursos</h2>
        <button className={styles.createBtn} onClick={onNewClick}>
          <i className="fas fa-plus"></i> Novo Curso
        </button>
      </div>

      <div className={styles.tableWrapper}>
        <Table 
          data={courses}
          columns={columns}
          // [CORREÇÃO]: Usando publicId como chave única do React
          keyExtractor={(course) => course.publicId} 
          isLoading={isLoading}
          emptyMessage="Nenhum curso cadastrado ainda."
        />
      </div>
    </div>
  );
};