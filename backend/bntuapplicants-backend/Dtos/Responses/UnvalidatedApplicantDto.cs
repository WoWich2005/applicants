namespace bntuapplicants_backend.Dtos.Responses
{
    public class UnvalidatedApplicantDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Validated { get; set; }
    }
}
