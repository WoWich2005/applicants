using System.ComponentModel.DataAnnotations;

namespace bntuapplicants_backend.Dtos.Requests
{
    public class ChangePasswordRequestDto
    {
        [Required]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = "";
    }
}
