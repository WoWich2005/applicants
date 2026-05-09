namespace bntuapplicants_backend.Dtos.Responses
{
    public class InvalidCompetitionListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FacultyName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
    }
}
