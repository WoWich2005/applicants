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
    [Route("/api/v1/evaluation_criteria_groups")]
    public class EvaluationCriteriaGroupController : ControllerBase
    {
        private readonly IEvaluationCriteriaGroupRepository _repository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public EvaluationCriteriaGroupController(IEvaluationCriteriaGroupRepository repository, IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<List<EvaluationCriteriaGroup>>> GetAllRecords()
        {
            var records = await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<EvaluationCriteriaGroup>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? idSearch = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            return Ok(await _repository.GetPagedAsync(page, pageSize, search, idSearch, sortField, sortOrder));
        }

        [HttpGet("{id}/delete-check")]
        public async Task<ActionResult<List<AdmissionCategoryPathDto>>> GetDeleteCheck(int id)
        {
            var records = await _repository.GetAdmissionCategoriesUsingGroupAsync(id);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EvaluationCriteriaGroup>> GetById(int id)
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
        public async Task<ActionResult<EvaluationCriteriaGroup>> Create([FromBody] EvaluationCriteriaGroupRequestDto dto)
        {
            if (await _repository.ExistsByNameAsync(dto.Name))
                return Conflict(new { message = (string)_localizer["EvaluationCriteriaGroup.NameExists"] });

            var createdRecord = await _repository.CreateAsync(new EvaluationCriteriaGroup
            {
                Name = dto.Name
            });

            if (createdRecord == null)
                return StatusCode(500, new { message = (string)_localizer["EvaluationCriteriaGroup.CreateError"] });

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
        public async Task<IActionResult> Update(int id, [FromBody] EvaluationCriteriaGroupRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["EvaluationCriteriaGroup.NotFound", id] });

            if (await _repository.ExistsByNameAsync(dto.Name, id))
                return Conflict(new { message = (string)_localizer["EvaluationCriteriaGroup.NameExists"] });

            bool success = await _repository.UpdateAsync(new EvaluationCriteriaGroup
            {
                Id = id,
                Name = dto.Name
            });

            if (!success)
                return StatusCode(500, new { message = (string)_localizer["EvaluationCriteriaGroup.UpdateError"] });

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
                return NotFound(new { message = (string)_localizer["EvaluationCriteriaGroup.NotFound", id] });

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, new { message = (string)_localizer["EvaluationCriteriaGroup.DeleteError"] });

            return NoContent();
        }
    }
}
