using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class EvaluationCriteriaGroupItemRequestDto
    {
        [Required(ErrorMessage = "Id группы обязателен")]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "Id оценочного параметра обязателен")]
        public int CriteriaId { get; set; }

        [Required(ErrorMessage = "Приоритет обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "Приоритет должен быть натуральным числом")]
        public int Priority { get; set; }
    }
}
