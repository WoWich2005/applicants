using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IApplicantEvaluationValueRepository
    {
        Task<ApplicantEvaluationValue?> CreateAsync(ApplicantEvaluationValue record);
        Task<List<ApplicantEvaluationValue>> GetAllByApplicantAsync(int applicantId);
        Task<ApplicantEvaluationValue?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ApplicantEvaluationValue record);
        Task<bool> DeleteAsync(int id);
    }
}
