using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class EvaluationCriteriaGroupItem
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int CriteriaId { get; set; }
        public int Priority { get; set; }
    }
}
