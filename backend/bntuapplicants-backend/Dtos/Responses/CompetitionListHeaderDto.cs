namespace bntuapplicants_backend.Dtos.Responses
{
    public class CompetitionListHeaderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string FacultyName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string SpecialtyName { get; set; } = "";
        public int Plan { get; set; }
        public List<CategoryHeaderDto> Categories { get; set; } = [];
    }

    public class CategoryHeaderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Quota { get; set; }
        public int AdmittedCount { get; set; }
        public int NotAdmittedCount { get; set; }
        public List<CriterionInfoDto> Criteria { get; set; } = [];
        public Dictionary<int, int>? MinScores { get; set; }
    }
}
