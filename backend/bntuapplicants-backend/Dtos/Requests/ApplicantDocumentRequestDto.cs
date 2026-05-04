using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class ApplicantDocumentRequestDto
    {
        [Required(ErrorMessage = "Id абитуриента обязательно")]
        public int ApplicantId { get; set; }

        [Required(ErrorMessage = "Id документа обязательно")]
        public int DocumentId { get; set; }

        [Required(ErrorMessage = "Количество баллов обязательно для заполнения")]
        [Range(-1000000000, 1000000000, ErrorMessage = "Значение должно быть от 1 до 1000000000")]
        public int PointsNumber { get; set; }
    }
}
