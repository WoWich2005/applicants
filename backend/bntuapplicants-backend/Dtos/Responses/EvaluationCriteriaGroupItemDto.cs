namespace bntuapplicants_backend.Dtos.Responses
{
    public class EvaluationCriteriaGroupItemDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int CriteriaId { get; set; }
        public int Priority { get; set; }
        public string CriteriaName { get; set; } = string.Empty;
    }
}
