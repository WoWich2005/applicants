using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("/api/v1/departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentRepository _repository;
        private readonly IFacultyRepository _facultyRepository;

        public DepartmentController(IDepartmentRepository repository, IFacultyRepository facultyRepository)
        {
            _repository = repository;
            _facultyRepository = facultyRepository;
        }

        private int? GetFacultyManagerFacultyId()
        {
            if (User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.FacultyManager)
                return null;
            var claim = User.FindFirst("faculty_id")?.Value;
            return claim != null ? int.Parse(claim) : null;
        }

        [HttpGet]
        public async Task<ActionResult<List<Department>>> GetAllRecords()
        {
            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue)
                return Ok(await _repository.GetByFacultyIdAsync(facultyId.Value));

            return Ok(await _repository.GetAllAsync());
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<Department>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? facultyId = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var managerFacultyId = GetFacultyManagerFacultyId();
            var effectiveFacultyId = managerFacultyId ?? facultyId;

            if (effectiveFacultyId.HasValue)
                return Ok(await _repository.GetPagedByFacultyIdAsync(effectiveFacultyId.Value, page, pageSize, search, sortField, sortOrder));

            return Ok(await _repository.GetPagedAsync(page, pageSize, search, sortField, sortOrder));
        }

        [HttpGet("by-faculty/{facultyId}")]
        public async Task<ActionResult<List<Department>>> GetByFacultyId(int facultyId)
        {
            var managerFacultyId = GetFacultyManagerFacultyId();
            if (managerFacultyId.HasValue && managerFacultyId.Value != facultyId)
                return Forbid();

            var records = await _repository.GetByFacultyIdAsync(facultyId);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);
            if (record == null)
                return NotFound();

            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && record.FacultyId != facultyId.Value)
                return Forbid();

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Department>> Create([FromBody] DepartmentRequestDto dto)
        {
            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && dto.FacultyId != facultyId.Value)
                return Forbid();

            var faculty = await _facultyRepository.GetByIdAsync(dto.FacultyId);
            if (faculty == null)
                return NotFound($"Факультет с id {dto.FacultyId} не найден");

            if (await _repository.ExistsByNameAsync(dto.Name, dto.FacultyId))
                return Conflict("Кафедра с таким названием уже существует в данном факультете");

            var createdRecord = await _repository.CreateAsync(new Department
            {
                Name = dto.Name,
                FacultyId = dto.FacultyId
            });

            if (createdRecord == null)
                return StatusCode(500, "Ошибка. Не удалось создать кафедру");

            return CreatedAtAction(
                nameof(this.GetById),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Кафедра с id {id} не найдена");

            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && (existing.FacultyId != facultyId.Value || dto.FacultyId != facultyId.Value))
                return Forbid();

            var faculty = await _facultyRepository.GetByIdAsync(dto.FacultyId);
            if (faculty == null)
                return NotFound($"Факультет с id {dto.FacultyId} не найден");

            if (await _repository.ExistsByNameAsync(dto.Name, dto.FacultyId, id))
                return Conflict("Кафедра с таким названием уже существует в данном факультете");

            bool success = await _repository.UpdateAsync(new Department
            {
                Id = id,
                Name = dto.Name,
                FacultyId = dto.FacultyId
            });

            if (!success)
                return StatusCode(500, "Ошибка при обновлении кафедры");

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Кафедра с id {id} не найдена");

            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && existing.FacultyId != facultyId.Value)
                return Forbid();

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, "Ошибка при удалении кафедры");

            return NoContent();
        }
    }
}
