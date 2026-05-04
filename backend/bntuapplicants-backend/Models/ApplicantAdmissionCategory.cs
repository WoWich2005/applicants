using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class ApplicantAdmissionCategory
    {
        public int Id { get; set; }
        public int ApplicantId { get; set; }
        public int AdmissionCategoryId { get; set; }
        public int SelectionPriority { get; set; }
    }
}
