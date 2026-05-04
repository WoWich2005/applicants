using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class DepartmentRequestDto
    {
        [Required(ErrorMessage = "Название кафедры обязательно")]
        [StringLength(255, ErrorMessage = "Название не должно превышать 255 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Факультет обязателен")]
        public int FacultyId { get; set; }
    }
}
