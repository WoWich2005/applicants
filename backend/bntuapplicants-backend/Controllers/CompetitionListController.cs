using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Controllers
{

    [ApiController]
    [Route("/api/v1/competition_lists")]
    public class CompetitionListController : ControllerBase
    {
        private readonly ICompetitionListRepository _repository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public CompetitionListController(
            ICompetitionListRepository repository,
            ISpecialtyRepository specialtyRepository,
            IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _specialtyRepository = specialtyRepository;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<List<CompetitionList>>> GetAllRecords()
        {
            var records = await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("by-specialty/{specialtyId}")]
        public async Task<ActionResult<List<CompetitionList>>> GetBySpecialtyId(int specialtyId)
        {
            var records = await _repository.GetBySpecialtyIdAsync(specialtyId);
            return Ok(records);
        }

        [HttpGet("by-specialty/{specialtyId}/paged")]
        public async Task<ActionResult<PagedResponse<CompetitionList>>> GetPagedBySpecialty(
            int specialtyId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            return Ok(await _repository.GetPagedBySpecialtyAsync(specialtyId, page, pageSize, search, sortField, sortOrder));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CompetitionList>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
                return NotFound();

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CompetitionList>> Create([FromBody] CompetitionListRequestDto dto)
        {
            var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
            if (specialty == null)
                return NotFound(new { message = (string)_localizer["Specialty.NotFound", dto.SpecialtyId] });

            if (await _repository.ExistsByNameAsync(dto.Name, dto.SpecialtyId))
                return Conflict(new { message = (string)_localizer["CompetitionList.NameExists"] });

            var createdRecord = await _repository.CreateAsync(new CompetitionList
            {
                Name = dto.Name,
                Plan = dto.Plan,
                SpecialtyId = dto.SpecialtyId
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["CompetitionList.CreateError"] });

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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] CompetitionListRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["CompetitionList.NotFound", id] });

            var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
            if (specialty == null)
                return NotFound(new { message = (string)_localizer["Specialty.NotFound", dto.SpecialtyId] });

            if (await _repository.ExistsByNameAsync(dto.Name, dto.SpecialtyId, id))
                return Conflict(new { message = (string)_localizer["CompetitionList.NameExists"] });

            bool success = await _repository.UpdateAsync(new CompetitionList
            {
                Id = id,
                Name = dto.Name,
                Plan = dto.Plan,
                SpecialtyId = dto.SpecialtyId
            });

            if (!success)
                return StatusCode(500, new { message = (string)_localizer["CompetitionList.UpdateError"] });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["CompetitionList.NotFound", id] });

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["CompetitionList.DeleteError"] });

            return NoContent();
        }
    }
}
