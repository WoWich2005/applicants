using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class ApplicantEvaluationValueRequestDto
    {
        [Required(ErrorMessage = "Id абитуриента обязателен")]
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "Id оценочного параметра обязателен")]
        public int EvaluationCriteriaId { get; set; }

        [Required(ErrorMessage = "Значение параметра обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Значение должно быть натуральным числом")]
        public int Value { get; set; }
    }
}
