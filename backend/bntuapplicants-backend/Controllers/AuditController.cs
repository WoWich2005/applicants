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
    [Route("/api/v1/audit")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository _auditRepo;
        private readonly IAuditLogger _auditLogger;
        private readonly IApplicantDeletionRequestRepository _deletionRequestRepo;
        private readonly SelectionService _selectionService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public AuditController(
            IAuditRepository auditRepo,
            IAuditLogger auditLogger,
            IApplicantDeletionRequestRepository deletionRequestRepo,
            SelectionService selectionService,
            IStringLocalizer<SharedResources> localizer)
        {
            _auditRepo = auditRepo;
            _auditLogger = auditLogger;
            _deletionRequestRepo = deletionRequestRepo;
            _selectionService = selectionService;
            _localizer = localizer;
        }

        // История конкретной записи — доступно всем авторизованным
        [HttpGet("entity-history")]
        public async Task<ActionResult<PagedResponse<AuditLogEntry>>> GetEntityHistory(
            [FromQuery] string entityType,
            [FromQuery] string entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? username = null,
            [FromQuery] string? action = null,
            [FromQuery] string? logEntityType = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? sortOrder = null)
        {
            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
                return BadRequest(new { message = "entityType and entityId are required" });

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return Ok(await _auditRepo.GetEntityHistoryAsync(entityType, entityId, page, pageSize, username, action, logEntityType, from, to, sortOrder));
        }

        // Общий журнал — все аутентифицированные
        [HttpGet("log")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<AuditLogEntry>>> GetAuditLog(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? username = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return Ok(await _auditRepo.GetAuditLogPagedAsync(page, pageSize, username, entityType, action, entityId, from, to, sortOrder));
        }

        [HttpGet("auth-log")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<AuthLogEntry>>> GetAuthLog(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? userId = null,
            [FromQuery] string? username = null,
            [FromQuery] string? eventType = null,
            [FromQuery] string? ipAddress = null,
            [FromQuery] string? failureReason = null,
            [FromQuery] string? userAgent = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? sortOrder = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return Ok(await _auditRepo.GetAuthLogPagedAsync(page, pageSize, userId, username, eventType, ipAddress, failureReason, userAgent, from, to, sortOrder));
        }

        // Статус валидации абитуриента
        [HttpGet("validation/{applicantId:int}")]
        [Authorize]
        public async Task<ActionResult<ValidationStatusDto>> GetValidationStatus(int applicantId)
        {
            var status = await _auditRepo.GetValidationStatusAsync(applicantId);
            if (status == null)
                return NotFound(new { message = (string)_localizer["Audit.ApplicantNotFound"] });
            return Ok(status);
        }

        // Подтвердить валидацию
        [HttpPost("validation/{applicantId:int}")]
        [Authorize(Roles = UserRoles.WriteAudit)]
        public async Task<IActionResult> Validate(int applicantId, [FromBody] ValidateApplicantRequestDto dto)
        {
            var (canValidate, missingCriteria, invalidPriorities) = await _auditRepo.CheckValidationBlockersAsync(applicantId);

            if (!canValidate)
                return BadRequest(new { missingCriteria, invalidPriorities });

            await _auditRepo.ValidateApplicantAsync(applicantId);
            await _auditLogger.LogValidateAsync(applicantId, dto.Comment);

            return NoContent();
        }

        // Отозвать валидацию
        [HttpDelete("validation/{applicantId:int}")]
        [Authorize(Roles = UserRoles.WriteAudit)]
        public async Task<IActionResult> Invalidate(int applicantId, [FromBody] InvalidateApplicantRequestDto dto)
        {
            await _auditRepo.InvalidateApplicantAsync(applicantId);
            await _auditLogger.LogInvalidateAsync(applicantId, automatic: false, dto.Comment);

            return NoContent();
        }

        // Счётчики алертов — все аутентифицированные
        [HttpGet("alerts-summary")]
        [Authorize]
        public async Task<ActionResult<AlertsSummaryDto>> GetAlertsSummary()
        {
            return Ok(await _auditRepo.GetAlertsSummaryAsync());
        }

        // Список абитуриентов (все + с фильтром по статусу)
        [HttpGet("unvalidated")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<UnvalidatedApplicantDto>>> GetUnvalidated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return Ok(await _auditRepo.GetUnvalidatedApplicantsAsync(page, pageSize, status, search));
        }

        // Список абитуриентов с некорректными данными
        [HttpGet("incomplete")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<IncompleteApplicantDto>>> GetIncomplete(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            return Ok(await _auditRepo.GetIncompleteApplicantsAsync(page, pageSize, search));
        }

        [HttpGet("incomplete/{applicantId}")]
        [Authorize]
        public async Task<ActionResult<IncompleteApplicantDto>> GetIncompleteForApplicant(int applicantId)
        {
            var (_, missingCriteria, invalidPriorities) = await _auditRepo.CheckValidationBlockersAsync(applicantId);
            return Ok(new IncompleteApplicantDto
            {
                Id = applicantId,
                MissingCriteria = missingCriteria,
                HasInvalidPriorities = invalidPriorities,
            });
        }

        // Конкурсные списки с некорректными приоритетами категорий приёма
        [HttpGet("invalid-admission-categories")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<InvalidCompetitionListDto>>> GetInvalidAdmissionCategories(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
            [FromQuery] string? faculty = null, [FromQuery] string? department = null, [FromQuery] string? specialty = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            return Ok(await _auditRepo.GetInvalidAdmissionCategoriesAsync(page, pageSize, search, faculty, department, specialty));
        }

        // Группы оценочных параметров с некорректными приоритетами
        [HttpGet("invalid-criteria-groups")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<InvalidCriteriaGroupDto>>> GetInvalidCriteriaGroups(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            return Ok(await _auditRepo.GetInvalidCriteriaGroupsAsync(page, pageSize, search));
        }

        // Список ожидающих удаления абитуриентов
        [HttpGet("pending-deletions")]
        [Authorize]
        public async Task<ActionResult<PagedResponse<PendingDeletionDto>>> GetPendingDeletions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? requestedBySearch = null,
            [FromQuery] string? status = null,
            [FromQuery] string? idSearch = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;
            return Ok(await _deletionRequestRepo.GetPagedAsync(page, pageSize, search, requestedBySearch, status, idSearch));
        }

        // Подтвердить удаление
        [HttpPost("pending-deletions/{applicantId:int}/confirm")]
        [Authorize(Roles = UserRoles.WriteAudit)]
        public async Task<IActionResult> ConfirmDeletion(int applicantId)
        {
            var confirmed = await _deletionRequestRepo.ConfirmAsync(applicantId);
            if (!confirmed)
                return NotFound(new { message = (string)_localizer["Applicant.DeletionRequest.NotFound"] });

            await _selectionService.RecalculateAllAsync();

            return NoContent();
        }

        // Отклонить удаление
        [HttpPost("pending-deletions/{applicantId:int}/reject")]
        [Authorize(Roles = UserRoles.WriteAudit)]
        public async Task<IActionResult> RejectDeletion(int applicantId)
        {
            var rejected = await _deletionRequestRepo.RejectAsync(applicantId);
            if (!rejected)
                return NotFound(new { message = (string)_localizer["Applicant.DeletionRequest.NotFound"] });

            await _selectionService.RecalculateAllAsync();

            return NoContent();
        }
    }
}
