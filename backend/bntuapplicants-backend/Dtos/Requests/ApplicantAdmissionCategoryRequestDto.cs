using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class ApplicantAdmissionCategoryRequestDto
    {
        [Required(ErrorMessage = "ApplicantAdmissionCategory.ApplicantId.Required")]
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "ApplicantAdmissionCategory.AdmissionCategoryId.Required")]
        public int AdmissionCategoryId { get; set; }

        [Required(ErrorMessage = "ApplicantAdmissionCategory.SelectionPriority.Required")]
        [Range(1, int.MaxValue, ErrorMessage = "ApplicantAdmissionCategory.SelectionPriority.Range")]
        public int SelectionPriority { get; set; }
    }
}
