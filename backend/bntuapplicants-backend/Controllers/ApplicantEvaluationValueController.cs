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
    [Route("/api/v1/applicant_evaluation_values")]
    public class ApplicantEvaluationValueController : ControllerBase
    {
        private readonly IApplicantEvaluationValueRepository _repository;
        private readonly IEvaluationCriteriaRepository _criteriaRepository;
        private readonly SelectionService _selectionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ApplicantEvaluationValueController(
            IApplicantEvaluationValueRepository repository,
            IEvaluationCriteriaRepository criteriaRepository,
            SelectionService selectionService,
            IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _criteriaRepository = criteriaRepository;
            _selectionService = selectionService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<ApplicantEvaluationValueDto>>> GetAllByApplicant(
            [FromQuery] int applicantId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string? sortField = null, [FromQuery] string? sortOrder = null,
            [FromQuery] string? id = null, [FromQuery] string? criteria = null, [FromQuery] string? value = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            return Ok(await _repository.GetAllByApplicantPagedAsync(applicantId, page, pageSize, sortField, sortOrder, id, criteria, value));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApplicantEvaluationValue>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
                return NotFound();

            return record;
        }

        private async Task<ActionResult?> ValidateValueRange(int criteriaId, int value)
        {
            var criteria = await _criteriaRepository.GetByIdAsync(criteriaId);
            if (criteria == null)
                return BadRequest(new { message = (string)_localizer["EvaluationCriteria.NotFoundSimple"] });

            if (value < criteria.MinValue || value > criteria.MaxValue)
                return BadRequest(new { message = (string)_localizer["ApplicantEvaluationValue.ValueOutOfRange", value, criteria.MinValue, criteria.MaxValue, criteria.Name] });

            return null;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.WriteApplicants)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApplicantEvaluationValue>> Create([FromBody] ApplicantEvaluationValueRequestDto dto)
        {
            var rangeError = await ValidateValueRange(dto.EvaluationCriteriaId, dto.Value);
            if (rangeError != null)
                return rangeError;

            if (await _repository.ExistsForApplicantAsync(dto.ApplicantId, dto.EvaluationCriteriaId))
                return BadRequest(new { message = (string)_localizer["ApplicantEvaluationValue.Duplicate"] });

            var createdRecord = await _repository.CreateAsync(new ApplicantEvaluationValue
            {
                ApplicantId = dto.ApplicantId,
                EvaluationCriteriaId = dto.EvaluationCriteriaId,
                Value = dto.Value
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["ApplicantEvaluationValue.CreateError"] });

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
        public async Task<IActionResult> Update(int id, [FromBody] ApplicantEvaluationValueRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Record.NotFound", id] });

            var rangeError = await ValidateValueRange(dto.EvaluationCriteriaId, dto.Value);
            if (rangeError != null)
                return rangeError;

            if (await _repository.ExistsForApplicantAsync(dto.ApplicantId, dto.EvaluationCriteriaId, excludeId: id))
                return BadRequest(new { message = (string)_localizer["ApplicantEvaluationValue.Duplicate"] });

            bool success = await _repository.UpdateAsync(new ApplicantEvaluationValue
            {
                Id = id,
                ApplicantId = dto.ApplicantId,
                EvaluationCriteriaId = dto.EvaluationCriteriaId,
                Value = dto.Value
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
