using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Models
{
    
    public class CompetitionList
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Plan { get; set; }
        public int SpecialtyId { get; set; }
    }
}
