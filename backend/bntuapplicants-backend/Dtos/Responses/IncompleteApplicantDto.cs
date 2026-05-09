namespace bntuapplicants_backend.Dtos.Responses
{
    public class IncompleteApplicantDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = "";
        public string Name { get; set; } = "";
        public List<string> MissingCriteria { get; set; } = [];
        public bool HasInvalidPriorities { get; set; }
    }
}
