using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("/api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly JwtService _jwtService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AuthController(IUserRepository userRepo, JwtService jwtService, IStringLocalizer<SharedResources> localizer)
        {
            _userRepo = userRepo;
            _jwtService = jwtService;
            _localizer = localizer;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
        {
            var user = await _userRepo.GetByUsernameAsync(dto.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = (string)_localizer["Auth.InvalidCredentials"] });

            if (!user.IsActive)
                return Unauthorized(new { message = (string)_localizer["Auth.AccountDeactivated"] });

            var specialtyIds = await _userRepo.GetSpecialtyIdsAsync(user.Id);
            var facultyAccessIds = await _userRepo.GetFacultyAccessIdsAsync(user.Id);
            var token = _jwtService.GenerateToken(user, specialtyIds, facultyAccessIds);

            return Ok(new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Role = user.Role,
                FacultyId = user.FacultyId,
                SpecialtyIds = specialtyIds,
                FacultyAccessIds = facultyAccessIds
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return NotFound();

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                return BadRequest(new { message = (string)_localizer["Auth.WrongCurrentPassword"] });

            var newHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _userRepo.ChangePasswordAsync(userId, newHash);
            return NoContent();
        }
    }
}
