using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Profiles.Admin.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that handles administrative operations related to student profiles.
    /// </summary>
    public interface IAdminStudentService
    {
        /// <summary>
        /// Retrieves a list of all student profiles.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="StudentDto"/> objects.</returns>
        Task<List<StudentDto>> GetAllStudentsAsync();


        Task<StudentDto> GetStudentByIdAsync(Guid id);
    }
}
