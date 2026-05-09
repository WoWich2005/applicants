using System.ComponentModel.DataAnnotations;

namespace bntuapplicants_backend.Dtos.Requests
{
    public class InvalidateApplicantRequestDto
    {
        [Required(ErrorMessage = "Audit.InvalidateCommentRequired")]
        public string Comment { get; set; } = "";
    }
}
