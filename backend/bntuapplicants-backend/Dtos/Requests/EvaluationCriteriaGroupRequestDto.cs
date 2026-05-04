using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class EvaluationCriteriaGroupRequestDto
    {
        [Required(ErrorMessage = "EvaluationCriteriaGroup.Name.Required")]
        [StringLength(255, ErrorMessage = "EvaluationCriteriaGroup.Name.MaxLength255")]
        public string Name { get; set; }
    }
}
