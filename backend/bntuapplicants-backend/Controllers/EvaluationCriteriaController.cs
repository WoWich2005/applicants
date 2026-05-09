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
    [Route("/api/v1/evaluation_criteria")]
    public class EvaluationCriteriaController : ControllerBase
    {
        private readonly IEvaluationCriteriaRepository _repository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public EvaluationCriteriaController(IEvaluationCriteriaRepository repository, IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<List<EvaluationCriteria>>> GetAllRecords()
        {
            var records = await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<EvaluationCriteria>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? type = null,
            [FromQuery] string? idSearch = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            return Ok(await _repository.GetPagedAsync(page, pageSize, search, type, idSearch, sortField, sortOrder));
        }

        [HttpGet("{id}/delete-check")]
        public async Task<ActionResult<EvaluationCriteriaDeleteCheckDto>> GetDeleteCheck(int id)
        {
            var result = await _repository.GetDeleteCheckAsync(id);
            return Ok(result);
        }

        [HttpGet("{id}/range-check")]
        public async Task<ActionResult<EvaluationCriteriaRangeCheckDto>> GetRangeCheck(int id, [FromQuery] int minValue, [FromQuery] int maxValue)
        {
            var result = await _repository.GetRangeCheckAsync(id, minValue, maxValue);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EvaluationCriteria>> GetById(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
                return NotFound();

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EvaluationCriteria>> Create([FromBody] EvaluationCriteriaRequestDto dto)
        {
            if (await _repository.ExistsByNameAsync(dto.Name))
                return Conflict(new { message = (string)_localizer["EvaluationCriteria.NameExists"] });

            var createdRecord = await _repository.CreateAsync(new EvaluationCriteria
            {
                Name = dto.Name,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                Type = dto.Type
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["EvaluationCriteria.CreateError"] });

            return CreatedAtAction(
                nameof(this.GetById),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] EvaluationCriteriaRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["EvaluationCriteria.NotFound", id] });

            if (await _repository.ExistsByNameAsync(dto.Name, id))
                return Conflict(new { message = (string)_localizer["EvaluationCriteria.NameExists"] });

            bool success = await _repository.UpdateAsync(new EvaluationCriteria
            {
                Id = id,
                Name = dto.Name,
                MinValue = dto.MinValue,
                MaxValue = dto.MaxValue,
                Type = dto.Type
            });

            if (!success)
                return StatusCode(500, new { message = (string)_localizer["EvaluationCriteria.UpdateError"] });

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["EvaluationCriteria.NotFound", id] });

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["EvaluationCriteria.DeleteError"] });

            return NoContent();
        }
    }
}
