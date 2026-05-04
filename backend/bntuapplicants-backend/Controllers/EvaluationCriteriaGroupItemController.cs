using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Controllers
{
    
    [ApiController]
    [Route("/api/v1/evaluation_criteria_group_items")]
    public class EvaluationCriteriaGroupItemController : ControllerBase
    {
        private readonly IEvaluationCriteriaGroupItemRepository _repository;
        private readonly IEvaluationCriteriaGroupRepository _groupRepository;

        public EvaluationCriteriaGroupItemController(
            IEvaluationCriteriaGroupItemRepository repository,
            IEvaluationCriteriaGroupRepository groupRepository)
        {
            _repository = repository;
            _groupRepository = groupRepository;
        }

        [HttpGet]
        public async Task<ActionResult<List<EvaluationCriteriaGroupItem>>> GetAllByGroup([FromQuery] int groupId)
        {
            var records = await _repository.GetAllByGroupAsync(groupId);
            return Ok(records);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EvaluationCriteriaGroupItem>> GetById(int id)
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
        public async Task<ActionResult<EvaluationCriteriaGroupItem>> Create([FromBody] EvaluationCriteriaGroupItemRequestDto dto)
        {
            var group = await _groupRepository.GetByIdAsync(dto.GroupId);
            if (group == null)
                return NotFound($"Группа оценочных параметров с id {dto.GroupId} не найдена");

            var createdRecord = await _repository.CreateAsync(new EvaluationCriteriaGroupItem
            {
                GroupId = dto.GroupId,
                CriteriaId = dto.CriteriaId,
                Priority = dto.Priority
            });

            if (createdRecord == null)
                return StatusCode(500, "Ошибка. Не удалось добавить оценочный параметр в группу");

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
        public async Task<IActionResult> Update(int id, [FromBody] EvaluationCriteriaGroupItemRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Запись с id {id} не найдена");

            bool success = await _repository.UpdateAsync(new EvaluationCriteriaGroupItem
            {
                Id = id,
                GroupId = dto.GroupId,
                CriteriaId = dto.CriteriaId,
                Priority = dto.Priority
            });

            if (!success)
                return StatusCode(500, "Ошибка при обновлении записи");

            return NoContent();
        }

        [HttpDelete("{id}")]
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

            return NoContent();
        }
    }
}
