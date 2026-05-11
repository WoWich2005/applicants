using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Controllers
{

    [ApiController]
    [Route("/api/v1/admission_categories")]
    public class AdmissionCategoryController : ControllerBase
    {
        private readonly IAdmissionCategoryRepository _repository;
        private readonly ICompetitionListRepository _competitionListRepository;
        private readonly SelectionService _selectionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AdmissionCategoryController(
            IAdmissionCategoryRepository repository,
            ICompetitionListRepository competitionListRepository,
            SelectionService selectionService,
            IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _competitionListRepository = competitionListRepository;
            _selectionService = selectionService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<List<AdmissionCategory>>> GetAll([FromQuery] int? competitionListId = null)
        {
            var records = competitionListId.HasValue
                ? await _repository.GetAllByCompetitionListAsync(competitionListId.Value)
                : await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<AdmissionCategory>>> GetPaged(
            [FromQuery] int competitionListId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? groupId = null,
            [FromQuery] string? idSearch = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            return Ok(await _repository.GetPagedByCompetitionListAsync(competitionListId, page, pageSize, search, groupId, idSearch, sortField, sortOrder));
        }

        [HttpGet("by-specialty/{specialtyId}")]
        public async Task<ActionResult<List<AdmissionCategory>>> GetBySpecialtyId(int specialtyId)
        {
            var records = await _repository.GetAllBySpecialtyIdAsync(specialtyId);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AdmissionCategory>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
                return NotFound();

            return record;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AdmissionCategory>> Create([FromBody] AdmissionCategoryRequestDto dto)
        {
            var list = await _competitionListRepository.GetByIdAsync(dto.CompetitionListId);
            if (list == null)
                return NotFound(new { message = (string)_localizer["CompetitionList.NotFound", dto.CompetitionListId] });

            if (await _repository.ExistsByNameAsync(dto.Name, dto.CompetitionListId))
                return Conflict(new { message = (string)_localizer["AdmissionCategory.NameExists"] });

            var createdRecord = await _repository.CreateAsync(new AdmissionCategory
            {
                Name = dto.Name,
                CompetitionListId = dto.CompetitionListId,
                EvaluationCriteriaGroupId = dto.EvaluationCriteriaGroupId,
                Quota = dto.Quota,
                Priority = dto.Priority
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["AdmissionCategory.CreateError"] });

            await _selectionService.RecalculateAllAsync();

            return CreatedAtAction(
                nameof(this.GetById),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] AdmissionCategoryRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["AdmissionCategory.NotFound", id] });

            if (await _repository.ExistsByNameAsync(dto.Name, dto.CompetitionListId, id))
                return Conflict(new { message = (string)_localizer["AdmissionCategory.NameExists"] });

            bool success = await _repository.UpdateAsync(new AdmissionCategory
            {
                Id = id,
                Name = dto.Name,
                CompetitionListId = dto.CompetitionListId,
                EvaluationCriteriaGroupId = dto.EvaluationCriteriaGroupId,
                Quota = dto.Quota,
                Priority = dto.Priority
            });

            if (!success)
                return StatusCode(500, new { message = (string)_localizer["AdmissionCategory.UpdateError"] });

            await _selectionService.RecalculateAllAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["AdmissionCategory.NotFound", id] });

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["AdmissionCategory.DeleteError"] });

            await _selectionService.RecalculateAllAsync();

            return NoContent();
        }
    }
}
