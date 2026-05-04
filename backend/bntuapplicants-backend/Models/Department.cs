using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class Department
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int FacultyId { get; set; }
    }
}
