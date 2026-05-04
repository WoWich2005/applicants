using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class ApplicantAdmissionCategoryRequestDto
    {
        [Required(ErrorMessage = "Id абитуриента обязателен")]
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "Id категории приема обязателен")]
        public int AdmissionCategoryId { get; set; }

        [Required(ErrorMessage = "Приоритет выбора обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "Приоритет должен быть натуральным числом")]
        public int SelectionPriority { get; set; }
    }
}
