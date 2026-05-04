using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class Specialty
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required int DepartmentId { get; set; }
    }
}
