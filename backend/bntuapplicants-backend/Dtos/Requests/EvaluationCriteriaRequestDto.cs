using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class EvaluationCriteriaRequestDto
    {
        [Required(ErrorMessage = "EvaluationCriteria.Name.Required")]
        [StringLength(255, ErrorMessage = "EvaluationCriteria.Name.MaxLength255")]
        public string Name { get; set; }

        [Required(ErrorMessage = "EvaluationCriteria.MinValue.Required")]
        public int MinValue { get; set; }

        [Required(ErrorMessage = "EvaluationCriteria.MaxValue.Required")]
        public int MaxValue { get; set; }

        [Required(ErrorMessage = "EvaluationCriteria.Type.Required")]
        [EnumDataType(typeof(CriteriaType), ErrorMessage = "EvaluationCriteria.Type.Invalid")]
        public CriteriaType Type { get; set; }
    }
}
