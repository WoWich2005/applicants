namespace bntuapplicants_backend.Dtos.Responses
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = "";
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
