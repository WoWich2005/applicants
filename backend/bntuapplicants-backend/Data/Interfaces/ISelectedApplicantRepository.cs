using bntuapplicants_backend.Models;

namespace bntuapplicants_backend.Data.Interfaces
{
    public interface ISelectedApplicantRepository
    {
        Task<List<SelectedApplicant>> GetByCompetitionListIdAsync(int competitionListId);
        Task<List<SelectedApplicant>> GetByApplicantIdAsync(int applicantId);
    }
}
