using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class DepartmentRequestDto
    {
        [Required(ErrorMessage = "Department.Name.Required")]
        [StringLength(255, ErrorMessage = "Department.Name.MaxLength255")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Department.FacultyId.Required")]
        public int FacultyId { get; set; }
    }
}
