using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class AdmissionCategoryRequestDto
    {
        [Required(ErrorMessage = "AdmissionCategory.Name.Required")]
        [StringLength(255, ErrorMessage = "AdmissionCategory.Name.MaxLength255")]
        public string Name { get; set; }

        [Required(ErrorMessage = "AdmissionCategory.CompetitionListId.Required")]
        public int CompetitionListId { get; set; }

        [Required(ErrorMessage = "AdmissionCategory.EvaluationCriteriaGroupId.Required")]
        public int EvaluationCriteriaGroupId { get; set; }

        [Required(ErrorMessage = "AdmissionCategory.Quota.Required")]
        [Range(1, int.MaxValue, ErrorMessage = "AdmissionCategory.Quota.Range")]
        public int Quota { get; set; }

        [Required(ErrorMessage = "AdmissionCategory.Priority.Required")]
        [Range(1, int.MaxValue, ErrorMessage = "AdmissionCategory.Priority.Range")]
        public int Priority { get; set; }
    }
}
