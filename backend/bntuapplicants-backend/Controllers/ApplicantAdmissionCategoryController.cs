using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("/api/v1/applicant_admission_categories")]
    public class ApplicantAdmissionCategoryController : ControllerBase
    {
        private readonly IApplicantAdmissionCategoryRepository _repository;
        private readonly SelectionService _selectionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ApplicantAdmissionCategoryController(
            IApplicantAdmissionCategoryRepository repository,
            SelectionService selectionService,
            IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _selectionService = selectionService;
            _localizer = localizer;
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
        [Authorize(Roles = UserRoles.WriteApplicants)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApplicantAdmissionCategory>> Create([FromBody] ApplicantAdmissionCategoryRequestDto dto)
        {
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

            await _selectionService.RecalculateAllAsync();

            return CreatedAtAction(
                nameof(this.GetById),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.WriteApplicants)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ApplicantAdmissionCategoryRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Record.NotFound", id] });

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

            await _selectionService.RecalculateAllAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.WriteApplicants)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Record.NotFound", id] });

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["Record.DeleteError"] });

            await _selectionService.RecalculateAllAsync();

            return NoContent();
        }
    }
}
