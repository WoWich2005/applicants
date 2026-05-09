namespace bntuapplicants_backend.Dtos.Responses
{
    public class PendingDeletionDto
    {
        public int ApplicantId { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string? ApplicantExternalId { get; set; }
        public int? RequestedByUserId { get; set; }
        public string? RequestedByUsername { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
