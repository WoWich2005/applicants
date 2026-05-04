using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class ApplicantRequestDto
    {
        [Required(ErrorMessage = "Имя абитуриента обязательно")]
        [StringLength(255, ErrorMessage = "Имя не должно превышать 255 символов")]
        public string Name { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "ID абитуриента обязателен")]
        [StringLength(255, ErrorMessage = "ID не должен превышать 255 символов")]
        public string ExternalId { get; set; }
    }
}
