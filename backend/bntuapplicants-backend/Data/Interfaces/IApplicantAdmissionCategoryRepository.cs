using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface IApplicantAdmissionCategoryRepository
    {
        Task<ApplicantAdmissionCategory?> CreateAsync(ApplicantAdmissionCategory record);
        Task<List<ApplicantAdmissionCategory>> GetAllByApplicantAsync(int applicantId);
        Task<List<Applicant>> GetApplicantsByAdmissionCategoryAsync(int admissionCategoryId);
        Task<ApplicantAdmissionCategory?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(ApplicantAdmissionCategory record);
        Task<bool> DeleteAsync(int id);
    }
}
