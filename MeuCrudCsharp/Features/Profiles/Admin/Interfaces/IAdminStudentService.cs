using MeuCrudCsharp.Features.Profiles.Admin.Dtos;

namespace MeuCrudCsharp.Features.Profiles.Admin.Interfaces
{
    public interface IAdminStudentService
    {
        Task<List<StudentDto>> GetAllStudentsAsync();
    }
}
