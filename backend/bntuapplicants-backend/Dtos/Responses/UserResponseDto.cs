namespace bntuapplicants_backend.Dtos.Responses
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
