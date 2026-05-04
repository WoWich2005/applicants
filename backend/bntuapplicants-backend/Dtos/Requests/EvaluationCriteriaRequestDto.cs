using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Dtos.Requests
{

    public class EvaluationCriteriaRequestDto
    {
        [Required(ErrorMessage = "Название оценочного параметра обязательно")]
        [StringLength(255, ErrorMessage = "Название не должно превышать 255 символов")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Минимальное значение обязательно")]
        public int MinValue { get; set; }

        [Required(ErrorMessage = "Максимальное значение обязательно")]
        public int MaxValue { get; set; }

        [Required(ErrorMessage = "Тип обязателен")]
        [EnumDataType(typeof(CriteriaType), ErrorMessage = "Недопустимое значение типа")]
        public CriteriaType Type { get; set; }
    }
}
