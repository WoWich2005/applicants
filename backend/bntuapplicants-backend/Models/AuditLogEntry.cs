namespace bntuapplicants_backend.Models
{
    public class AuditLogEntry
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string Action { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string EntityId { get; set; } = "";
        public string? Changes { get; set; }
    }
}
