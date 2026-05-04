using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Models;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public ApplicantEvaluationValueController(
            IApplicantEvaluationValueRepository repository,
            IEvaluationCriteriaRepository criteriaRepository,
            SelectionService selectionService)
        {
            _repository = repository;
            _criteriaRepository = criteriaRepository;
            _selectionService = selectionService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ApplicantEvaluationValue>>> GetAllByApplicant([FromQuery] int applicantId)
        {
            var records = await _repository.GetAllByApplicantAsync(applicantId);
            return Ok(records);
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
                return BadRequest("Оценочный параметр не найден");

            if (value < criteria.MinValue || value > criteria.MaxValue)
                return BadRequest($"Значение {value} выходит за пределы допустимого диапазона [{criteria.MinValue}, {criteria.MaxValue}] для параметра «{criteria.Name}»");

            return null;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApplicantEvaluationValue>> Create([FromBody] ApplicantEvaluationValueRequestDto dto)
        {
            var rangeError = await ValidateValueRange(dto.EvaluationCriteriaId, dto.Value);
            if (rangeError != null)
                return rangeError;

            var createdRecord = await _repository.CreateAsync(new ApplicantEvaluationValue
            {
                ApplicantId = dto.ApplicantId,
                EvaluationCriteriaId = dto.EvaluationCriteriaId,
                Value = dto.Value
            });

            if (createdRecord == null)
                return StatusCode(500, "Ошибка. Не удалось добавить оценочный параметр для абитуриента");

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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ApplicantEvaluationValueRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Запись с id {id} не найдена");

            var rangeError = await ValidateValueRange(dto.EvaluationCriteriaId, dto.Value);
            if (rangeError != null)
                return rangeError;

            bool success = await _repository.UpdateAsync(new ApplicantEvaluationValue
            {
                Id = id,
                ApplicantId = dto.ApplicantId,
                EvaluationCriteriaId = dto.EvaluationCriteriaId,
                Value = dto.Value
            });

            if (!success)
                return StatusCode(500, "Ошибка при обновлении записи");

            await _selectionService.RecalculateForApplicantAsync(dto.ApplicantId);
            if (existing.ApplicantId != dto.ApplicantId)
                await _selectionService.RecalculateForApplicantAsync(existing.ApplicantId);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Запись с id {id} не найдена");

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, "Ошибка при удалении записи");

            await _selectionService.RecalculateForApplicantAsync(existing.ApplicantId);

            return NoContent();
        }
    }
}
