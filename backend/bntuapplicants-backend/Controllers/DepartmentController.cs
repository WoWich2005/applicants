using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("/api/v1/departments")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentRepository _repository;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public DepartmentController(IDepartmentRepository repository, IFacultyRepository facultyRepository, IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _facultyRepository = facultyRepository;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<List<Department>>> GetAllRecords()
        {
            return Ok(await _repository.GetAllAsync());
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<Department>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? facultyId = null,
            [FromQuery] string? idSearch = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            if (facultyId.HasValue)
                return Ok(await _repository.GetPagedByFacultyIdAsync(facultyId.Value, page, pageSize, search, idSearch, sortField, sortOrder));

            return Ok(await _repository.GetPagedAsync(page, pageSize, search, idSearch, sortField, sortOrder));
        }

        [HttpGet("by-faculty/{facultyId}")]
        public async Task<ActionResult<List<Department>>> GetByFacultyId(int facultyId)
        {
            var records = await _repository.GetByFacultyIdAsync(facultyId);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Department>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);
            if (record == null)
                return NotFound();
            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.WriteStructure)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Department>> Create([FromBody] DepartmentRequestDto dto)
        {
            var faculty = await _facultyRepository.GetByIdAsync(dto.FacultyId);
            if (faculty == null)
                return NotFound(new { message = (string)_localizer["Faculty.NotFound", dto.FacultyId] });

            if (await _repository.ExistsByNameAsync(dto.Name, dto.FacultyId))
                return Conflict(new { message = (string)_localizer["Department.NameExists"] });

            var createdRecord = await _repository.CreateAsync(new Department
            {
                Name = dto.Name,
                FacultyId = dto.FacultyId
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["Department.CreateError"] });

            return CreatedAtAction(
                nameof(this.GetById),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.WriteStructure)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] DepartmentRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Department.NotFound", id] });

            var faculty = await _facultyRepository.GetByIdAsync(dto.FacultyId);
            if (faculty == null)
                return NotFound(new { message = (string)_localizer["Faculty.NotFound", dto.FacultyId] });

            if (await _repository.ExistsByNameAsync(dto.Name, dto.FacultyId, id))
                return Conflict(new { message = (string)_localizer["Department.NameExists"] });

            bool success = await _repository.UpdateAsync(new Department
            {
                Id = id,
                Name = dto.Name,
                FacultyId = dto.FacultyId
            });

            if (!success)
                return StatusCode(500, new { message = (string)_localizer["Department.UpdateError"] });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.WriteStructure)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Department.NotFound", id] });

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["Department.DeleteError"] });

            return NoContent();
        }
    }
}
