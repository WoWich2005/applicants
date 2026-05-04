using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class CompetitionListRequestDto
    {
        [Required(ErrorMessage = "CompetitionList.Name.Required")]
        [StringLength(255, ErrorMessage = "CompetitionList.Name.MaxLength255")]
        public string Name { get; set; }

        [Required(ErrorMessage = "CompetitionList.Plan.Required")]
        [Range(1, int.MaxValue, ErrorMessage = "CompetitionList.Plan.Range")]
        public int Plan { get; set; }

        [Required(ErrorMessage = "CompetitionList.SpecialtyId.Required")]
        public int SpecialtyId { get; set; }
    }
}
