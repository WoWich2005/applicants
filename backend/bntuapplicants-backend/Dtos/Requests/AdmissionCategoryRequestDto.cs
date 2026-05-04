using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class AdmissionCategoryRequestDto
    {
        [Required(ErrorMessage = "Название категории приема обязательно")]
        [StringLength(255, ErrorMessage = "Название не должно превышать 255 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Id конкурсного списка обязателен")]
        public int CompetitionListId { get; set; }

        [Required(ErrorMessage = "Id группы оценочных параметров обязателен")]
        public int EvaluationCriteriaGroupId { get; set; }

        [Required(ErrorMessage = "Квота обязательна")]
        [Range(1, int.MaxValue, ErrorMessage = "Квота должна быть натуральным числом")]
        public int Quota { get; set; }

        [Required(ErrorMessage = "Приоритет обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "Приоритет должен быть натуральным числом")]
        public int Priority { get; set; }
    }
}
