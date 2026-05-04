using bntuapplicants_backend.Data.Interfaces;
using bntuapplicants_backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Controllers
{

    [Route("/get_results")]
    [ApiController]
    public class ResultController : ControllerBase
    {
        private readonly IResultRepository _resultRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IFacultyRepository _facultyRepository;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public ResultController(
            IResultRepository resultRepository,
            ISpecialtyRepository specialtyRepository,
            IFacultyRepository facultyRepository,
            IStringLocalizer<SharedResources> localizer)
        {
            _resultRepository = resultRepository;
            _specialtyRepository = specialtyRepository;
            _facultyRepository = facultyRepository;
            _localizer = localizer;
        }

        [HttpGet("{facultyId}")]
        public async Task<ActionResult> GetByFacultyID(int facultyId)
        {
            var faculty = await _facultyRepository.GetByIdAsync(facultyId);
            if (faculty == null)
                return NotFound(new { message = (string)_localizer["Result.FacultyNotFound", facultyId] });

            var facultySpecialties = await _specialtyRepository.GetByFacultyIdAsync(facultyId);
            if (!facultySpecialties.Any())
                return NotFound(new { message = (string)_localizer["Result.NoSpecialties"] });

            var specialtyIdToApplicants = await _resultRepository.GetByFacultyIdAsync(facultyId);

            var excelService = new ExcelExportService();
            var excelBytes = await excelService.GenerateAdmissionReportAsync(
                facultyId,
                facultySpecialties,
                specialtyIdToApplicants
            );
            var fileName = $"Зачисленные_{faculty.Name}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}
