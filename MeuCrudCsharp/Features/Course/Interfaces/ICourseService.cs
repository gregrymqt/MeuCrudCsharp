using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Course.DTOs;
using MeuCrudCsharp.Features.Exceptions;
using MeuCrudCsharp.Features.Videos.DTOs;

namespace MeuCrudCsharp.Features.Courses.Interfaces
{
    /// <summary>
    /// Define o contrato para o serviço de gerenciamento de cursos.
    /// </summary>
    public interface ICourseService
    {
        /// <summary>
        /// Obtém uma lista de todos os cursos, incluindo seus vídeos associados.
        /// </summary>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém a lista de DTOs de curso.</returns>
        Task<PaginatedResultDto<CourseDto>> GetCoursesWithVideosPaginatedAsync(
            int pageNumber,
            int pageSize
        );

        /// <summary>
        /// Busca um curso específico pelo seu ID, incluindo seus vídeos.
        /// </summary>
        /// <param name="id">O ID do curso a ser buscado.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém o DTO do curso encontrado.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        Task<CourseDto?> GetCourseByIdAsync(Guid id);

        /// <summary>
        /// Cria um novo curso com base nos dados fornecidos.
        /// </summary>
        /// <param name="createDto">O DTO com os dados para a criação do curso.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém o DTO do curso recém-criado.</returns>
        /// <exception cref="AppServiceException">Lançada se já existir um curso com o mesmo nome ou se ocorrer outro erro de serviço.</exception>
        Task<CourseDto> CreateCourseAsync(CreateCourseDto createDto);

        /// <summary>
        /// Atualiza um curso existente.
        /// </summary>
        /// <param name="id">O ID do curso a ser atualizado.</param>
        /// <param name="updateDto">O DTO com os novos dados do curso.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona. O resultado da tarefa contém o DTO do curso atualizado.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        Task<CourseDto> UpdateCourseAsync(Guid id, UpdateCourseDto updateDto);

        /// <summary>
        /// Exclui um curso pelo seu ID.
        /// </summary>
        /// <param name="id">O ID do curso a ser excluído.</param>
        /// <returns>Uma tarefa que representa a operação assíncrona.</returns>
        /// <exception cref="ResourceNotFoundException">Lançada se o curso com o ID especificado não for encontrado.</exception>
        /// <exception cref="AppServiceException">Lançada se o curso tiver vídeos associados e não puder ser excluído.</exception>
        Task DeleteCourseAsync(Guid id);
    }
}
