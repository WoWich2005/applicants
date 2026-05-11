namespace bntuapplicants_backend.Dtos.Responses
{
    public class CompetitionListSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string FacultyName { get; set; } = "";
        public string DepartmentName { get; set; } = "";
        public string SpecialtyName { get; set; } = "";
        public int Plan { get; set; }
        public int ApplicationsCount { get; set; }
        public int SelectedCount { get; set; }
    }
}
