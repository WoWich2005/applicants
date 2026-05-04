using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class ApplicantEvaluationValue
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public int EvaluationCriteriaId { get; set; }
        public int Value { get; set; }
    }
}
