using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{

    public class Applicant
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Notes { get; set; }
        public required string ExternalId { get; set; }
    }
}
