using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Dtos.Responses
{
    public class EvaluationCriteriaRangeCheckDto
    {
        public List<Applicant> Applicants { get; set; } = [];
        public int ApplicantTotalCount { get; set; }
    }
}
