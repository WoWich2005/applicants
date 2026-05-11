using bntuapplicants_backend.Constants;
using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace bntuapplicants_backend.Controllers
{
    [ApiController]
    [Route("api/v1/selection")]
    public class SelectionController : ControllerBase
    {
        private readonly SelectionService _selectionService;
        private readonly IResultRepository _resultRepository;
        private readonly ExcelExportService _excelService;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SelectionController(
            SelectionService selectionService,
            IResultRepository resultRepository,
            ExcelExportService excelService,
            IStringLocalizer<SharedResources> localizer)
        {
            _selectionService = selectionService;
            _resultRepository = resultRepository;
            _excelService = excelService;
            _localizer = localizer;
        }

        [HttpPost("recalculate")]
        [Authorize(Roles = UserRoles.WriteStructure)]
        public async Task<IActionResult> RecalculateAll()
        {
            await _selectionService.RecalculateAllAsync();
            return Ok(new { message = (string)_localizer["Selection.RecalculateSuccess"] });
        }

        [HttpGet("competition-lists")]
        public async Task<IActionResult> GetCompetitionLists(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? facultySearch = null,
            [FromQuery] string? departmentSearch = null,
            [FromQuery] string? specialtySearch = null,
            [FromQuery] int? selectedCount = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            var result = await _resultRepository.GetCompetitionListsPagedAsync(
                page, pageSize, search, facultySearch, departmentSearch, specialtySearch,
                selectedCount, sortField, sortOrder);
            return Ok(result);
        }

        [HttpGet("competition-lists/{id}/result")]
        public async Task<IActionResult> GetCompetitionListResult(int id)
        {
            var result = await _resultRepository.GetCompetitionListResultAsync(id);
            if (result == null)
                return NotFound(new { message = (string)_localizer["Selection.CompetitionListNotFound", id] });
            return Ok(result);
        }

        [HttpGet("competition-lists/{id}/header")]
        public async Task<IActionResult> GetCompetitionListHeader(int id)
        {
            var header = await _resultRepository.GetCompetitionListHeaderAsync(id);
            if (header == null)
                return NotFound(new { message = (string)_localizer["Selection.CompetitionListNotFound", id] });
            return Ok(header);
        }

        [HttpGet("competition-lists/{clId}/categories/{catId}/applicants")]
        public async Task<IActionResult> GetCategoryApplicants(
            int clId, int catId,
            [FromQuery] string type = "admitted",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? id = null,
            [FromQuery] string? externalId = null,
            [FromQuery] string? name = null,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortOrder = null)
        {
            bool admitted = type != "notAdmitted";
            var scoreFilters = Request.Query
                .Where(kv => kv.Key.StartsWith("score_") && int.TryParse(kv.Key[6..], out _))
                .ToDictionary(kv => int.Parse(kv.Key[6..]), kv => kv.Value.ToString());

            var result = await _resultRepository.GetCategoryApplicantsPagedAsync(
                clId, catId, admitted, page, pageSize,
                id, externalId, name, scoreFilters, sortField, sortOrder);
            return Ok(result);
        }

        [HttpGet("competition-lists/{id}/excel")]
        public async Task<IActionResult> DownloadExcel(int id)
        {
            var result = await _resultRepository.GetCompetitionListResultAsync(id);
            if (result == null)
                return NotFound(new { message = (string)_localizer["Selection.CompetitionListNotFound", id] });

            var bytes = await _excelService.GenerateCompetitionListReportAsync(result);
            static string Sanitize(string s) =>
                string.Join("_", s.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
            var fileName = $"{Sanitize(result.FacultyName)}.{Sanitize(result.DepartmentName)}.{Sanitize(result.SpecialtyName)}.{Sanitize(result.Name)}.{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
    }
}
