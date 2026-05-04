using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class AdmissionCategory
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int CompetitionListId { get; set; }
        public int EvaluationCriteriaGroupId { get; set; }
        public int Quota { get; set; }
        public int Priority { get; set; }
    }
}
