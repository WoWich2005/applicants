using System.ComponentModel.DataAnnotations;

namespace bntuapplicants_backend.Dtos.Requests
{
    public class LoginRequestDto
    {
        [Required]
        public string Username { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
