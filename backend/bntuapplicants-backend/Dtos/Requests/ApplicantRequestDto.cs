using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class ApplicantRequestDto
    {
        [Required(ErrorMessage = "Applicant.Name.Required")]
        [StringLength(255, ErrorMessage = "Applicant.Name.MaxLength255")]
        public string Name { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Applicant.ExternalId.Required")]
        [StringLength(255, ErrorMessage = "Applicant.ExternalId.MaxLength255")]
        public string ExternalId { get; set; }
    }
}
