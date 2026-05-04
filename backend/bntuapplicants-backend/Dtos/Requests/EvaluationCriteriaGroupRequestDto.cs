using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class EvaluationCriteriaGroupRequestDto
    {
        [Required(ErrorMessage = "Название группы оценочных параметров обязательно")]
        [StringLength(255, ErrorMessage = "Название не должно превышать 255 символов")]
        public string Name { get; set; }
    }
}
