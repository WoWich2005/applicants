using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class EvaluationCriteriaGroupItemRequestDto
    {
        [Required(ErrorMessage = "EvaluationCriteriaGroupItem.GroupId.Required")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "EvaluationCriteriaGroupItem.CriteriaId.Required")]
        public int CriteriaId { get; set; }

        [Required(ErrorMessage = "EvaluationCriteriaGroupItem.Priority.Required")]
        [Range(1, int.MaxValue, ErrorMessage = "EvaluationCriteriaGroupItem.Priority.Range")]
        public int Priority { get; set; }
    }
}
