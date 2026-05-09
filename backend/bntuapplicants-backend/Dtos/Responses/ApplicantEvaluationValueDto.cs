namespace bntuapplicants_backend.Dtos.Responses
{
    public class ApplicantEvaluationValueDto
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public int EvaluationCriteriaId { get; set; }
        public int Value { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
    }
}
