using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{
    
    public class SpecialtyRequestDto
    {
        [Required(ErrorMessage = "Имя специальности обязательно")]
        [StringLength(100, ErrorMessage = "Имя специальности не должно превышать 100 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Специальность должна принадлежать кафедре")]
        public int DepartmentId { get; set; }
    }
}
