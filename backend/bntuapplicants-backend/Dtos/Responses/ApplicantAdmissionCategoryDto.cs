namespace bntuapplicants_backend.Dtos.Responses
{
    public class ApplicantAdmissionCategoryDto
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public int AdmissionCategoryId { get; set; }
        public int SelectionPriority { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public string CompetitionListName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }
}
