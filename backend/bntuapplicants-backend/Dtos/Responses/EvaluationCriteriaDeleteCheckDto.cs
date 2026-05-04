using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Dtos.Responses
{
    public class EvaluationCriteriaDeleteCheckDto
    {
        public List<EvaluationCriteriaGroup> UsedInGroups { get; set; } = [];
        public List<Applicant> Applicants { get; set; } = [];
        public int ApplicantTotalCount { get; set; }
    }
}
