using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("/api/v1/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _repo;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public UserController(IUserRepository repo, IStringLocalizer<SharedResources> localizer)
        {
            _repo = repo;
            _localizer = localizer;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] string? idSearch = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var (items, total) = await _repo.GetPagedAsync(page, pageSize, search, role, isActive, idSearch, sortField, sortOrder);
            return Ok(new { items, total });
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> GetById(int id)
        {
            var user = await _repo.GetDetailedByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequestDto dto)
        {
            var existing = await _repo.GetByUsernameAsync(dto.Username);
            if (existing != null)
                return BadRequest(new { message = (string)_localizer["User.UsernameExists"] });

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role,
                IsActive = true,
                MustChangePassword = false
            };

            var created = await _repo.CreateAsync(user);
            var result = await _repo.GetDetailedByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequestDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var conflicting = await _repo.GetByUsernameAsync(dto.Username);
            if (conflicting != null && conflicting.Id != id)
                return BadRequest(new { message = (string)_localizer["User.UsernameExists"] });

            var user = new User
            {
                Id = id,
                Username = dto.Username,
                Role = dto.Role,
                PasswordHash = !string.IsNullOrEmpty(dto.Password)
                    ? BCrypt.Net.BCrypt.HashPassword(dto.Password)
                    : ""
            };

            await _repo.UpdateAsync(user);
            return NoContent();
        }

        [HttpPatch("{id}/toggle-active")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();
            await _repo.ToggleActiveAsync(id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (id == currentUserId)
                return BadRequest(new { message = (string)_localizer["User.CannotDeleteSelf"] });

            if (existing.Role == UserRoles.SuperAdmin)
            {
                var count = await _repo.CountByRoleAsync(UserRoles.SuperAdmin);
                if (count <= 1)
                    return BadRequest(new { message = (string)_localizer["User.CannotDeleteLastSuperAdmin"] });
            }

            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
