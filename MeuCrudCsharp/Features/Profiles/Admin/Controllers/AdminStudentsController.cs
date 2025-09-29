using System;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Profiles.Admin.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MeuCrudCsharp.Features.Profiles.Admin.Controllers
{
    /// <summary>
    /// Manages administrative operations related to student profiles.
    /// Requires 'Admin' role for access.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("api/admin/students")]
    public class AdminStudentsController : ApiControllerBase
    {
        private readonly IAdminStudentService _studentService;
        private readonly ILogger<AdminStudentsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminStudentsController"/> class.
        /// </summary>
        /// <param name="studentService">The service responsible for student-related business logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public AdminStudentsController(
            IAdminStudentService studentService,
            ILogger<AdminStudentsController> logger
        )
        {
            _studentService = studentService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves a list of all students.
        /// </summary>
        /// <returns>A list of all student profiles.</returns>
        /// <response code="200">Returns the list of students.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not in the 'Admin' role.</response>
        /// <response code="500">If an unexpected server error occurs.</response>
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
                _logger.LogError(ex, "An error occurred while fetching students.");
                return StatusCode(500, "An internal error occurred while fetching students.");
            }
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(Guid id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                return Ok(student);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching students.");
                return StatusCode(500, "An internal error occurred while fetching students.");
            }
        }
    }
}
