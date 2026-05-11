namespace bntuapplicants_backend.Dtos.Responses
{
    public class CompetitionListResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string FacultyName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string SpecialtyName { get; set; } = "";
        public int Plan { get; set; }
        public List<CategoryResultDto> Categories { get; set; } = [];
    }

    public class CategoryResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Quota { get; set; }
        public int Priority { get; set; }
        public List<CriterionInfoDto> Criteria { get; set; } = [];
        public List<ApplicantResultDto> Admitted { get; set; } = [];
        public List<ApplicantResultDto> NotAdmitted { get; set; } = [];
    }

    public class CriterionInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class ApplicantResultDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = "";
        public string Name { get; set; } = "";
        public string? AdmittedTo { get; set; }
        // criteriaId -> value
        public Dictionary<int, int> Scores { get; set; } = [];
    }
}
