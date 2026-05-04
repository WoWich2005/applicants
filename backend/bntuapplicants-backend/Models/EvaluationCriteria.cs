using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    public enum CriteriaType
    {
        HigherIsBetter,
        LowerIsBetter
    }

    public class EvaluationCriteria
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public CriteriaType Type { get; set; }
    }
}
