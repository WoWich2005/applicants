using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class ApplicantEvaluationValueRequestDto
    {
        [Required(ErrorMessage = "ApplicantEvaluationValue.ApplicantId.Required")]
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "ApplicantEvaluationValue.EvaluationCriteriaId.Required")]
        public int EvaluationCriteriaId { get; set; }

        [Required(ErrorMessage = "ApplicantEvaluationValue.Value.Required")]
        [Range(1, int.MaxValue, ErrorMessage = "ApplicantEvaluationValue.Value.Range")]
        public int Value { get; set; }
    }
}
