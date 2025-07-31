using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Profiles.Admin.Controllers
{
    [ApiController]
    [Route("api/admin/students")]
    // [Authorize(Roles = "Admin")]
    public class AdminStudentsController : ControllerBase
    {
        private readonly IAdminStudentService _studentService;

        public AdminStudentsController(IAdminStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                var students = await _studentService.GetAllStudentsAsync();
                return Ok(students);
            }
            catch (Exception ex)
            {
                // Log do erro (ex.Message)
                return StatusCode(500, "Ocorreu um erro interno ao buscar os alunos.");
            }
        }
    }
}
