namespace bntuapplicants_backend.Models
{
    public class AuthLogEntry
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string EventType { get; set; } = "";
        public string? FailureReason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
