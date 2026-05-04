using System.ComponentModel.DataAnnotations;

namespace bntuapplicants_backend.Dtos.Requests
{
    public class CreateUserRequestDto
    {
        [Required]
        public string Username { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";

        public int? FacultyId { get; set; }

        public List<int> SpecialtyIds { get; set; } = [];

        public List<int> FacultyAccessIds { get; set; } = [];
    }
}
