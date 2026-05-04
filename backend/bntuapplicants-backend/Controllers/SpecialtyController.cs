using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace bntuapplicants_backend.Controllers
{
    [Route("/api/v1/specialties")]
    [ApiController]
    public class SpecialtyController : ControllerBase
    {
        private readonly ISpecialtyRepository _repository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SpecialtyController(ISpecialtyRepository repository, IDepartmentRepository departmentRepository, IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _departmentRepository = departmentRepository;
            _localizer = localizer;
        }

        private int? GetFacultyManagerFacultyId()
        {
            if (User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.FacultyManager)
                return null;
            var claim = User.FindFirst("faculty_id")?.Value;
            return claim != null ? int.Parse(claim) : null;
        }

        private async Task<bool> SpecialtyBelongsToFacultyAsync(int specialtyId, int facultyId)
        {
            var specialty = await _repository.GetByIdAsync(specialtyId);
            if (specialty == null) return false;
            var department = await _departmentRepository.GetByIdAsync(specialty.DepartmentId);
            return department?.FacultyId == facultyId;
        }

        private async Task<bool> DepartmentBelongsToFacultyAsync(int departmentId, int facultyId)
        {
            var department = await _departmentRepository.GetByIdAsync(departmentId);
            return department?.FacultyId == facultyId;
        }

        [HttpGet]
        public async Task<ActionResult<List<Specialty>>> GetAllRecords()
        {
            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue)
            {
                var filtered = await _repository.GetByFacultyIdAsync(facultyId.Value);
                return Ok(filtered);
            }

            var records = await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<Specialty>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? facultyId = null,
            [FromQuery] int? departmentId = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var managerFacultyId = GetFacultyManagerFacultyId();

            if (departmentId.HasValue)
            {
                if (managerFacultyId.HasValue && !await DepartmentBelongsToFacultyAsync(departmentId.Value, managerFacultyId.Value))
                    return Forbid();
                return Ok(await _repository.GetPagedByDepartmentIdAsync(departmentId.Value, page, pageSize, search, sortField, sortOrder));
            }

            var effectiveFacultyId = managerFacultyId ?? facultyId;
            if (effectiveFacultyId.HasValue)
                return Ok(await _repository.GetPagedByFacultyIdAsync(effectiveFacultyId.Value, page, pageSize, search, sortField, sortOrder));

            return Ok(await _repository.GetPagedAsync(page, pageSize, search, sortField, sortOrder));
        }

        [HttpGet("by-department/{departmentId}")]
        public async Task<ActionResult<List<Specialty>>> GetByDepartmentId(int departmentId)
        {
            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && !await DepartmentBelongsToFacultyAsync(departmentId, facultyId.Value))
                return Forbid();

            var records = await _repository.GetByDepartmentIdAsync(departmentId);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Specialty>> GetByID(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
                return NotFound();

            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && !await DepartmentBelongsToFacultyAsync(record.DepartmentId, facultyId.Value))
                return Forbid();

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<Faculty>> Create([FromBody] SpecialtyRequestDto dto)
        {
            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && !await DepartmentBelongsToFacultyAsync(dto.DepartmentId, facultyId.Value))
                return Forbid();

            if (await _repository.ExistsByNameAsync(dto.Name, dto.DepartmentId))
                return Conflict(new { message = (string)_localizer["Specialty.NameExists"] });

            var createdRecord = await _repository.CreateAsync(new Specialty()
            {
                Name = dto.Name,
                DepartmentId = dto.DepartmentId
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["Specialty.CreateError"] });

            return CreatedAtAction(
                nameof(this.GetByID),
                new { id = createdRecord.Id },
                createdRecord
            );
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
                return NotFound(new { message = (string)_localizer["Specialty.NotFound", id] });

            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue && !await DepartmentBelongsToFacultyAsync(existing.DepartmentId, facultyId.Value))
                return Forbid();

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["Specialty.DeleteError"] });

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] SpecialtyRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Specialty.NotFound", id] });

            var facultyId = GetFacultyManagerFacultyId();
            if (facultyId.HasValue)
            {
                if (!await DepartmentBelongsToFacultyAsync(existing.DepartmentId, facultyId.Value))
                    return Forbid();
                if (!await DepartmentBelongsToFacultyAsync(dto.DepartmentId, facultyId.Value))
                    return Forbid();
            }

            if (await _repository.ExistsByNameAsync(dto.Name, dto.DepartmentId, id))
                return Conflict(new { message = (string)_localizer["Specialty.NameExists"] });

            bool success = await _repository.UpdateAsync(new Specialty
            {
                Id = id,
                Name = dto.Name,
                DepartmentId = dto.DepartmentId
            });
            if (!success)
                return StatusCode(500, new { message = (string)_localizer["Specialty.UpdateError"] });

            return NoContent();
        }
    }
}
