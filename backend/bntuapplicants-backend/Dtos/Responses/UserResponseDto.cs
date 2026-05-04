namespace bntuapplicants_backend.Dtos.Responses
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public int? FacultyId { get; set; }
        public string? FacultyName { get; set; }
        public bool IsActive { get; set; }
        public List<int> SpecialtyIds { get; set; } = [];
        public List<int> FacultyAccessIds { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }
}
