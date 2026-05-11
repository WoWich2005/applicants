namespace bntuapplicants_backend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
