using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class SpecialtyRequestDto
    {
        [Required(ErrorMessage = "Specialty.Name.Required")]
        [StringLength(100, ErrorMessage = "Specialty.Name.MaxLength100")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Specialty.DepartmentId.Required")]
        public int DepartmentId { get; set; }
    }
}
