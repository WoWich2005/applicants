using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class SelectedApplicant
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public int AdmissionCategoryId { get; set; }
    }
}
