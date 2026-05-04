using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class CompetitionListRequestDto
    {
        [Required(ErrorMessage = "Название конкурсного списка обязательно")]
        [StringLength(255, ErrorMessage = "Название не должно превышать 255 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "План набора обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "План набора должен быть натуральным числом")]
        public int Plan { get; set; }

        [Required(ErrorMessage = "Id специальности обязателен")]
        public int SpecialtyId { get; set; }
    }
}
