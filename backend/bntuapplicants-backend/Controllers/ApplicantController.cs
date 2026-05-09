using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Dtos.Requests;
using bntuapplicants_backend.Dtos.Responses;
using bntuapplicants_backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Npgsql;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace bntuapplicants_backend.Controllers
{
    [Route("/api/v1/applicants")]
    [ApiController]
    public class ApplicantController : ControllerBase
    {
        private readonly IApplicantRepository _repository;
        private readonly IApplicantDeletionRequestRepository _deletionRequestRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ApplicantController(
            IApplicantRepository repository,
            IApplicantDeletionRequestRepository deletionRequestRepository,
            IStringLocalizer<SharedResources> localizer)
        {
            _repository = repository;
            _deletionRequestRepository = deletionRequestRepository;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<ActionResult<List<Applicant>>> GetAllRecords()
        {
            var records = await _repository.GetAllAsync();
            return Ok(records);
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResponse<Applicant>>> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? externalIdSearch = null,
            [FromQuery] string? idSearch = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            return Ok(await _repository.GetPagedAsync(page, pageSize, search, externalIdSearch, idSearch, sortField, sortOrder));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Applicant>> GetByID(int id)
        {
            var record = await _repository.GetByIdAsync(id);

            if (record == null)
            {
                return NotFound();
            }

            return record;
        }

        [HttpPost]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Faculty>> Create([FromBody] ApplicantRequestDto dto)
        {
            try
            {
                var createdRecord = await _repository.CreateAsync(new Applicant()
                {
                    Name = dto.Name,
                    Notes = dto.Notes,
                    ExternalId = dto.ExternalId
                });

                if (createdRecord == null)
                    return StatusCode(500, new { message = (string)_localizer["Applicant.CreateError"] });

                return CreatedAtAction(
                    nameof(this.GetByID),
                    new { id = createdRecord.Id },
                    createdRecord
                );
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                var (existing, isDeleted) = await _repository.FindByExternalIdAsync(dto.ExternalId!);
                if (existing != null && isDeleted)
                {
                    return Conflict(new
                    {
                        message = (string)_localizer["Applicant.ExternalIdExistsDeleted"],
                        isDeleted = true,
                        deletedApplicant = new
                        {
                            id = existing.Id,
                            name = existing.Name,
                            notes = existing.Notes,
                            externalId = existing.ExternalId
                        }
                    });
                }
                return Conflict(new { message = (string)_localizer["Applicant.ExternalIdExists"] });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Applicant.NotFound", id] });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) is { } uid ? int.Parse(uid) : (int?)null;
            var requested = await _deletionRequestRepository.RequestAsync(id, userId);
            if (!requested)
                return Conflict(new { message = (string)_localizer["Applicant.DeletionRequest.AlreadyPending"] });

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.FacultyManager + "," + UserRoles.AdmissionsOperator)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] ApplicantRequestDto dto)
        {
            var existing = await _repository.GetByIdAsync(id);
            if (existing == null)
                return NotFound(new { message = (string)_localizer["Applicant.NotFound", id] });

            try
            {
                bool success = await _repository.UpdateAsync(new Applicant
                {
                    Id = id,
                    Name = dto.Name,
                    Notes = dto.Notes,
                    ExternalId = dto.ExternalId
                });
                if (!success)
                    return StatusCode(500, new { message = (string)_localizer["Applicant.UpdateError"] });

                return NoContent();
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return Conflict(new { message = (string)_localizer["Applicant.ExternalIdExists"] });
            }
        }
    }
}
