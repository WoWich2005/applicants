using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Controllers
{
    
    [ApiController]
    [Route("/api/v1/faculties")]
    public class FacultyController : ControllerBase
    {
        private readonly IFacultyRepository _repository;

        public FacultyController(IFacultyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<Faculty>>> GetAllRecords()
        {
            var records = await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<Faculty>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            return Ok(await _repository.GetPagedAsync(page, pageSize, search, sortField, sortOrder));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Faculty>> GetByID(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
            {
                return NotFound();
            }

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Faculty>> Create([FromBody] FacultyRequestDto dto)
        {
            if (await _repository.ExistsByNameAsync(dto.Name))
                return Conflict("Факультет с таким названием уже существует");

            var createdRecord = await _repository.CreateAsync(new Faculty()
            {
                Name = dto.Name
            });

            if (createdRecord == null)
                return StatusCode(500, "Ошибка. Не удалось создать факультет");

            return CreatedAtAction(
                nameof(this.GetByID),
                new { id = createdRecord.Id },
                createdRecord
            );
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Факультет с id {id} не найден");

            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return StatusCode(500, "Ошибка при удалении факультета");

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] FacultyRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound($"Факультет с id {id} не найден");

            if (await _repository.ExistsByNameAsync(dto.Name, id))
                return Conflict("Факультет с таким названием уже существует");

            bool success = await _repository.UpdateAsync(new Faculty
            {
                Id = id,
                Name = dto.Name
            });
            if (!success)
                return StatusCode(500, "Ошибка при обновлении факультета");

            return NoContent();
        }
    }
}
