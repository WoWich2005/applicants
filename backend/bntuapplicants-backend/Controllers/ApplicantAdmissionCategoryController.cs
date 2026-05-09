using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("/api/v1/applicant_admission_categories")]
    public class ApplicantAdmissionCategoryController : ControllerBase
    {
        private readonly IApplicantAdmissionCategoryRepository _repository;
        private readonly IAdmissionCategoryRepository _admissionCategoryRepository;
        private readonly ICompetitionListRepository _competitionListRepository;
        private readonly SelectionService _selectionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ApplicantAdmissionCategoryController(
            IApplicantAdmissionCategoryRepository repository,
            IAdmissionCategoryRepository admissionCategoryRepository,
            ICompetitionListRepository competitionListRepository,
            SelectionService selectionService,
            IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _admissionCategoryRepository = admissionCategoryRepository;
            _competitionListRepository = competitionListRepository;
            _selectionService = selectionService;
            _localizer = localizer;
        }

        private List<int>? GetOperatorSpecialtyIds()
        {
            if (User.FindFirst(ClaimTypes.Role)?.Value != UserRoles.AdmissionsOperator)
                return null;

            var claim = User.FindFirst("specialty_ids")?.Value;
            if (string.IsNullOrEmpty(claim)) return [];
            return claim.Split(',').Select(int.Parse).ToList();
        }

        private async Task<int?> GetSpecialtyIdForCategoryAsync(int admissionCategoryId)
        {
            var category = await _admissionCategoryRepository.GetByIdAsync(admissionCategoryId);
            if (category == null) return null;
            var list = await _competitionListRepository.GetByIdAsync(category.CompetitionListId);
            return list?.SpecialtyId;
        }

        private async Task<bool> HasSpecialtyAccessAsync(int admissionCategoryId)
        {
            var allowedIds = GetOperatorSpecialtyIds();
            if (allowedIds == null) return true;

            var specialtyId = await GetSpecialtyIdForCategoryAsync(admissionCategoryId);
            return specialtyId.HasValue && allowedIds.Contains(specialtyId.Value);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<ApplicantAdmissionCategoryDto>>> GetAllByApplicant(
            [FromQuery] int applicantId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string? sortField = null, [FromQuery] string? sortOrder = null,
            [FromQuery] string? id = null, [FromQuery] string? selectionPriority = null, [FromQuery] string? faculty = null, [FromQuery] string? department = null,
            [FromQuery] string? specialty = null, [FromQuery] string? competitionList = null,
            [FromQuery] string? category = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            var result = await _repository.GetAllByApplicantPagedAsync(
                applicantId, page, pageSize, sortField, sortOrder,
                id, selectionPriority, faculty, department, specialty, competitionList, category);
            return Ok(result);
        }

        [HttpGet("by-category/{admissionCategoryId}")]
        public async Task<ActionResult<List<Applicant>>> GetApplicantsByCategory(int admissionCategoryId)
        {
            var records = await _repository.GetApplicantsByAdmissionCategoryAsync(admissionCategoryId);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicantAdmissionCategory>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
                return NotFound();

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApplicantAdmissionCategory>> Create([FromBody] ApplicantAdmissionCategoryRequestDto dto)
        {
            if (!await HasSpecialtyAccessAsync(dto.AdmissionCategoryId))
                return Forbid();

            if (await _repository.ExistsAsync(dto.ApplicantId, dto.AdmissionCategoryId))
                return BadRequest(new { message = (string)_localizer["ApplicantAdmissionCategory.AlreadyExists"] });

            var createdRecord = await _repository.CreateAsync(new ApplicantAdmissionCategory
            {
                ApplicantId = dto.ApplicantId,
                AdmissionCategoryId = dto.AdmissionCategoryId,
                SelectionPriority = dto.SelectionPriority
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["ApplicantAdmissionCategory.CreateError"] });

            await _selectionService.RecalculateForApplicantAsync(dto.ApplicantId);

            return CreatedAtAction(
                nameof(this.GetById),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ApplicantAdmissionCategoryRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Record.NotFound", id] });

            if (!await HasSpecialtyAccessAsync(dto.AdmissionCategoryId))
                return Forbid();

            if (await _repository.ExistsAsync(dto.ApplicantId, dto.AdmissionCategoryId, id))
                return BadRequest(new { message = (string)_localizer["ApplicantAdmissionCategory.AlreadyExists"] });

            bool success = await _repository.UpdateAsync(new ApplicantAdmissionCategory
            {
                Id = id,
                ApplicantId = dto.ApplicantId,
                AdmissionCategoryId = dto.AdmissionCategoryId,
                SelectionPriority = dto.SelectionPriority
            });

            if (!success)
                return StatusCode(500, new { message = (string)_localizer["Record.UpdateError"] });

            await _selectionService.RecalculateForApplicantAsync(dto.ApplicantId);
            if (existing.ApplicantId != dto.ApplicantId)
                await _selectionService.RecalculateForApplicantAsync(existing.ApplicantId);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Record.NotFound", id] });

            if (!await HasSpecialtyAccessAsync(existing.AdmissionCategoryId))
                return Forbid();

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["Record.DeleteError"] });

            await _selectionService.RecalculateForApplicantAsync(existing.ApplicantId);

            return NoContent();
        }
    }
}
