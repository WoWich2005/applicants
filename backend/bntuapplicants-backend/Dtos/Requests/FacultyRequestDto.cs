using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class FacultyRequestDto
    {
        [Required(ErrorMessage = "Faculty.Name.Required")]
        [StringLength(100, ErrorMessage = "Faculty.Name.MaxLength100")]
        public string Name { get; set; }
    }
}
